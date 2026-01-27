using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum MovePattern { StraightDown, Sine, ZigZag }

    [Header("Stats")]
    public int maxHp = 3;
    public float speed = 2.5f;

    [Header("Movement (default = StraightDown)")]
    public MovePattern movePattern = MovePattern.StraightDown;
    public float xAmplitude = 1.5f;      // utilisé pour Sine/ZigZag
    public float sineFrequency = 2.0f;   // utilisé pour Sine
    public float zigzagSpeed = 2.0f;     // utilisé pour ZigZag

    [Header("FX")]
    public GameObject explosionPrefab;

    [Header("HP Bar")]
    public Transform hpFill;
    public bool hideBarWhenFull = true;

    int hp;
    float bottomY;

    Vector3 fillBasePos;
    Vector3 fillBaseScale;

    SpriteRenderer sr;
    Color baseColor;
    float flashTimer;

    // Movement state
    float spawnX;
    float t0;

    void OnEnable()
    {
        hp = maxHp;

        var cam = Camera.main;
        bottomY = (cam != null) ? (cam.transform.position.y - cam.orthographicSize - 2f) : -9999f;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        flashTimer = 0f;

        if (hpFill != null)
        {
            fillBasePos = hpFill.localPosition;
            fillBaseScale = hpFill.localScale;
            UpdateHpBar();
        }

        CaptureMoveOrigin();
    }

    void CaptureMoveOrigin()
    {
        spawnX = transform.position.x;
        t0 = Time.time;
    }

    public void ResetMovement()
    {
        movePattern = MovePattern.StraightDown;
        CaptureMoveOrigin();
    }

    public void SetMovement(MovePattern pattern, float amp, float freq, float zig)
    {
        movePattern = pattern;
        xAmplitude = amp;
        sineFrequency = freq;
        zigzagSpeed = zig;
        CaptureMoveOrigin();
    }

    void Update()
    {
        // Flash hit
        if (sr != null && flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = (flashTimer > 0f) ? Color.white : baseColor;
        }


        var route = GetComponent<EnemyRouteFollower>();
        if (route != null && route.HasRoute)
            return; // la route pilote le mouvement


        // Formation: ne pas bouger pendant l'entrée / ni en formation
        var member = GetComponent<EnemyFormationMember>();
        if (member != null && (member.isFlyingIn || member.inFormation))
            return;

        Vector3 p = transform.position;

        // Toujours descendre
        p += Vector3.down * speed * Time.deltaTime;

        // Mouvement horizontal optionnel
        float t = Time.time - t0;

        switch (movePattern)
        {
            case MovePattern.Sine:
                p.x = spawnX + Mathf.Sin(t * sineFrequency * Mathf.PI * 2f) * xAmplitude;
                break;

            case MovePattern.ZigZag:
                p.x = spawnX + (Mathf.PingPong(t * zigzagSpeed, xAmplitude * 2f) - xAmplitude);
                break;

            default:
                // StraightDown -> rien
                break;
        }

        transform.position = p;

        if (transform.position.y < bottomY)
            gameObject.SetActive(false);
    }

    public void TakeHit(int dmg)
    {
        hp -= dmg;
        flashTimer = 0.08f;

        UpdateHpBar();

        if (hp <= 0)
            Die();
    }

    void Die()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        gameObject.SetActive(false);
    }

    void UpdateHpBar()
    {
        if (hpFill == null) return;

        float ratio = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));

        Vector3 s = fillBaseScale;
        s.x = fillBaseScale.x * ratio;
        hpFill.localScale = s;

        float dx = (fillBaseScale.x - s.x) * 0.5f;
        hpFill.localPosition = new Vector3(fillBasePos.x - dx, fillBasePos.y, fillBasePos.z);

        if (hideBarWhenFull)
            hpFill.parent.gameObject.SetActive(hp < maxHp);
        else
            hpFill.parent.gameObject.SetActive(true);
    }
}
