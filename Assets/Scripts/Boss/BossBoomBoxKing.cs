using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossBoomBoxKing : MonoBehaviour, IDamageable
{
    public enum Phase { Phase1, Phase2, Phase3 }

    [Header("Boss")]
    public int maxHp = 500;

    [Header("Phase thresholds")]
    public int phase2Hp = 300;
    public int phase3Hp = 150;
    public int rewindTriggerHp = 100;

    [Header("Routes (useViewportPoints recommended)")]
    public EnemyRouteSO routePhase1;
    public EnemyRouteSO routePhase2Entry;
    public EnemyRouteSO routePhase2Loop;

    [Header("Attacks - Prefabs")]
    public BossProjectile soundWaveProjectilePrefab;
    public BossShockwave bassDropShockwavePrefab;
    public BossPunch speakerPunchPrefab;
    public BossProjectile cassetteProjectilePrefab;
    public BossProjectile volumeProjectilePrefab;

    [Header("Sound Wave")]
    public int soundWaveProjectileCount = 8;
    public float soundWaveSpeed = 3.0f;
    public int soundWaveDamage = 10;

    [Header("Bass Drop")]
    public float phase2BassDropInterval = 8f;
    public float phase3BassDropInterval = 5f;

    [Header("Cassette Eject")]
    public float cassetteInterval = 10f;
    public float cassetteSpeed = 4.0f;
    public int cassetteDamage = 20;

    [Header("Speaker Punch")]
    public float phase3PunchInterval = 1.2f;

    [Header("Volume Max")]
    public float volumeInterval = 15f;
    public float volumeChargeTime = 2.0f;
    public int volumeProjectileCount = 24;
    public float volumeSpeed = 4.0f;
    public int volumeDamage = 15;

    [Header("Phase 3 Teleport (Viewport coords)")]
    public float phase3TeleportInterval = 3.0f;
    public List<Vector2> teleportViewportPositions = new()
    {
        new Vector2(0.15f, 0.90f),
        new Vector2(0.85f, 0.90f),
        new Vector2(0.50f, 0.82f),
        new Vector2(0.15f, 0.62f),
        new Vector2(0.85f, 0.62f),
        new Vector2(0.50f, 0.45f),
        new Vector2(0.33f, 0.70f),
        new Vector2(0.67f, 0.70f),
    };

    int currentHp;
    Phase phase = (Phase)(-1);

    SpriteRenderer bossRenderer;
    EnemyRouteFollower follower;

    bool invulnerable;
    bool rewindUsed;
    bool punchAlternate;
  

    Coroutine soundWaveRoutine;
    Coroutine bassDropRoutine;
    Coroutine cassetteRoutine;
    Coroutine punchRoutine;
    Coroutine teleportRoutine;
    Coroutine volumeRoutine;

    void Start()
    {
        UnityEngine.Debug.Log(" BOSS REGISTER");
    }

    void Awake()
    {
        bossRenderer = GetComponent<SpriteRenderer>();
        follower = GetComponent<EnemyRouteFollower>();
    }

    void OnEnable()
    {
        currentHp = maxHp;
        invulnerable = false;
        rewindUsed = false;
        punchAlternate = false;

        StopPhaseCoroutines();

        phase = (Phase)(-1);
        StartPhase(Phase.Phase1);

    }

    void Update()
    {
        if (currentHp <= 0) return;

        if (!rewindUsed && currentHp <= rewindTriggerHp)
            StartCoroutine(RewindOnce());

        if (phase != Phase.Phase3 && currentHp <= phase3Hp)
            StartPhase(Phase.Phase3);
        else if (phase == Phase.Phase1 && currentHp <= phase2Hp)
            StartPhase(Phase.Phase2);
    }


    public void TakeHit(int dmg)
    {
        if (invulnerable) return;

        currentHp -= dmg;
        UnityEngine.Debug.Log("BOSS HIT : -" + dmg + " HP=" + currentHp);

        if (currentHp <= 0)
            Die();
    }

    void StartPhase(Phase next)
    {
        if (phase == next) return;
        phase = next;

        StopPhaseCoroutines();

        if (phase == Phase.Phase1)
        {
            if (follower != null && routePhase1 != null)
                follower.ApplyRoute(routePhase1);
        }
        else if (phase == Phase.Phase2)
        {
            // entry route then switch to loop on actionId
            if (follower != null && routePhase2Entry != null)
                follower.ApplyRoute(routePhase2Entry);

            soundWaveRoutine = StartCoroutine(LoopEvery(2f, SoundWave));
            bassDropRoutine = StartCoroutine(LoopEvery(phase2BassDropInterval, BassDrop));
        }
        else // Phase3
        {
            if (follower != null)
                follower.ClearRoute();

            soundWaveRoutine = StartCoroutine(LoopEvery(1f, SoundWave));
            bassDropRoutine = StartCoroutine(LoopEvery(phase3BassDropInterval, BassDrop));
            cassetteRoutine = StartCoroutine(LoopEvery(cassetteInterval, CassetteEject));
            volumeRoutine = StartCoroutine(LoopEvery(volumeInterval, () => StartCoroutine(VolumeMax())));
            punchRoutine = StartCoroutine(LoopEvery(phase3PunchInterval, () => SpeakerPunch(punchAlternate ? -1 : 1)));
            teleportRoutine = StartCoroutine(TeleportLoop());
        }
    }

    void StopPhaseCoroutines()
    {
        if (soundWaveRoutine != null) StopCoroutine(soundWaveRoutine);
        if (bassDropRoutine != null) StopCoroutine(bassDropRoutine);
        if (cassetteRoutine != null) StopCoroutine(cassetteRoutine);
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        if (teleportRoutine != null) StopCoroutine(teleportRoutine);
        if (volumeRoutine != null) StopCoroutine(volumeRoutine);

        soundWaveRoutine = null;
        bassDropRoutine = null;
        cassetteRoutine = null;
        punchRoutine = null;
        teleportRoutine = null;
        volumeRoutine = null;
    }

    // Called by EnemyRouteFollower when a waypoint actionId is hit
    public void OnRouteAction(string actionId)
    {
        if (string.IsNullOrEmpty(actionId))
            return;

        // Phase1: sound wave at each waypoint
        if (actionId.StartsWith("sound_wave"))
        {
            SoundWave();
            return;
        }

        // Phase2: switch entry -> loop on roar
        if (phase == Phase.Phase2 && actionId == "phase2_roar")
        {
            if (follower != null && routePhase2Loop != null)
                follower.ApplyRoute(routePhase2Loop);
            return;
        }

        if (actionId == "speaker_punch_left")
        {
            SpeakerPunch(-1);
            return;
        }

        if (actionId == "speaker_punch_right")
        {
            SpeakerPunch(1);
            return;
        }
    }

    void SoundWave()
    {
        if (soundWaveProjectilePrefab == null) return;

        Vector3 pos = transform.position;
        for (int i = 0; i < soundWaveProjectileCount; i++)
        {
            float a = (360f / soundWaveProjectileCount) * i;
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            var p = Instantiate(soundWaveProjectilePrefab, pos, Quaternion.identity);
            p.damage = soundWaveDamage;
            p.velocity = dir * soundWaveSpeed;
        }
    }

    void BassDrop()
    {
        if (bassDropShockwavePrefab == null) return;
        Instantiate(bassDropShockwavePrefab, transform.position, Quaternion.identity);
    }

    void SpeakerPunch(int side)
    {
        punchAlternate = !punchAlternate;

        if (speakerPunchPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float z = transform.position.z;
        float depth = Mathf.Abs(z - cam.transform.position.z);

        float x = (side < 0) ? -0.05f : 1.05f;
        float y = 0.5f;

        var playerT = FindPlayerTransform();
        if (playerT != null)
        {
            var vp = cam.WorldToViewportPoint(playerT.position);
            y = Mathf.Clamp(vp.y, 0.15f, 0.85f);
        }

        Vector3 target = cam.ViewportToWorldPoint(new Vector3(x, y, depth));
        target.z = z;

        var punch = Instantiate(speakerPunchPrefab);
        punch.damage = 15;
        punch.Fire(transform.position, target);
    }

    void CassetteEject()
    {
        if (cassetteProjectilePrefab == null) return;

        var playerT = FindPlayerTransform();

        var p = Instantiate(cassetteProjectilePrefab, transform.position, Quaternion.identity);
        p.damage = cassetteDamage;
        p.velocity = Vector2.down * cassetteSpeed;
        p.homing = (playerT != null);
        if (p.homing) p.SetHomingTarget(playerT);
        p.maxSpeed = Mathf.Max(p.maxSpeed, cassetteSpeed + 2f);
        p.rotateDegreesPerSecond = 360f;
    }

    IEnumerator VolumeMax()
    {
        if (volumeProjectilePrefab == null)
            yield break;

        invulnerable = true;
        yield return FlashAlpha(0.35f, volumeChargeTime);

        Vector3 pos = transform.position;
        for (int i = 0; i < volumeProjectileCount; i++)
        {
            float a = (360f / volumeProjectileCount) * i;
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            var p = Instantiate(volumeProjectilePrefab, pos, Quaternion.identity);
            p.damage = volumeDamage;
            p.velocity = dir * volumeSpeed;
        }

        invulnerable = false;
    }

    IEnumerator TeleportLoop()
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        float z = transform.position.z;
        float depth = Mathf.Abs(z - cam.transform.position.z);

        while (phase == Phase.Phase3 && currentHp > 0)
        {
            yield return Fade(1f, 0f, 0.25f);

            if (teleportViewportPositions != null && teleportViewportPositions.Count > 0)
            {
                var vp = teleportViewportPositions[UnityEngine.Random.Range(0, teleportViewportPositions.Count)];
                Vector3 w = cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, depth));
                w.z = z;
                transform.position = w;
            }

            yield return Fade(0f, 1f, 0.25f);

            float hold = Mathf.Max(0f, phase3TeleportInterval - 0.5f);
            yield return new WaitForSeconds(hold);
        }
    }

    IEnumerator RewindOnce()
    {
        rewindUsed = true;
        invulnerable = true;

        foreach (var proj in FindObjectsByType<BossProjectile>(FindObjectsSortMode.None))
            Destroy(proj.gameObject);
        foreach (var sw in FindObjectsByType<BossShockwave>(FindObjectsSortMode.None))
            Destroy(sw.gameObject);
        foreach (var p in FindObjectsByType<BossPunch>(FindObjectsSortMode.None))
            Destroy(p.gameObject);

        currentHp = Mathf.Min(maxHp, currentHp + 50);

        yield return FlashAlpha(0.2f, 2f);

        invulnerable = false;
    }

    IEnumerator FlashAlpha(float minAlpha, float duration)
    {
        if (bossRenderer == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            float a = Mathf.Lerp(minAlpha, 1f, Mathf.PingPong(t * 6f, 1f));
            var c = bossRenderer.color;
            c.a = a;
            bossRenderer.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        var cc = bossRenderer.color;
        cc.a = 1f;
        bossRenderer.color = cc;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (bossRenderer == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            float a = Mathf.Lerp(from, to, t / duration);
            var c = bossRenderer.color;
            c.a = a;
            bossRenderer.color = c;
            t += Time.deltaTime;
            yield return null;
        }

        var cc = bossRenderer.color;
        cc.a = to;
        bossRenderer.color = cc;
    }

    IEnumerator LoopEvery(float interval, System.Action action)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.15f, 0.55f));
        while (currentHp > 0)
        {
            action?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }

    Transform FindPlayerTransform()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) return ph.transform;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) return pc.transform;

        return null;
    }

    void Die()
    {
        UnityEngine.Debug.Log("BOSS DEAD");

        StopPhaseCoroutines();

        var tracker = GetComponent<WaveTrackedEnemy>();
        if (tracker != null)
            tracker.NotifyGone();

        gameObject.SetActive(false);
    }
}
