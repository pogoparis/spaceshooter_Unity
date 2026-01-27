using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveManager : MonoBehaviour
{
    public enum SpawnStyle { SingleRandomTop, FormationGrid }

    // Route en cours (appliquée à tous les spawns de la phase via TrackSpawned)
    private EnemyRouteSO currentPhaseRoute;

    [Serializable]
    public class WavePhase
    {
        [Header("Route (null => descente normale)")]
        public EnemyRouteSO route;

        public string name = "Phase";
        public SpawnStyle style = SpawnStyle.SingleRandomTop;

        [Header("Enemy")]
        public GameObject enemyPrefab;

        [Header("Counts")]
        public int count = 10;

        [Header("Timing")]
        public float startDelay = 1.0f;
        public float spawnInterval = 0.25f;

        [Header("Flow")]
        public bool waitForClear = true;

        [Header("Formation (si FormationGrid)")]
        [Tooltip("Important: >= 2 pour éviter une colonne qui sort de l'écran")]
        public int cols = 5;

        [Header("Formation Overrides (optionnel)")]
        public bool overrideFormationSettings = false;
        public float flyInDuration = 1.2f;
        public float arcSideOffset = 2.0f;
        public float arcDownAmount = 4.0f;
        public float spawnStagger = 0.12f;
        public float rowStaggerBonus = 0.25f;

        [Header("Safety (optionnel)")]
        public float spawnTimeout = 10f;
        public float clearTimeout = 120f;
    }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        public float startDelay = 1.0f;
        public List<WavePhase> phases = new();
    }

    [Header("Refs")]
    public EnemySpawner spawner;
    public FormationController formation;

    [Header("Waves")]
    public List<Wave> waves = new();

    [Header("Loop / Flow")]
    public bool loopForever = false;
    public float betweenWavesDelay = 1.0f;

    [Header("Difficulty Scaling")]
    public float countGrowthPerWave = 0.15f;
    public float intervalReductionPerWave = 0.08f;
    public float minSpawnInterval = 0.06f;

    [Header("Debug")]
    public bool debugLogs = false;

    // phase runtime state
    int aliveInPhase = 0;
    int spawnedInPhase = 0;
    int targetInPhase = 0;
    bool spawningDone = false;

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (formation == null) formation = FindFirstObjectByType<FormationController>();

        if (spawner != null) spawner.OnEnemySpawned += TrackSpawned;
        if (formation != null) formation.OnEnemySpawned += TrackSpawned;

        StartCoroutine(RunWaves());
    }

    void OnDestroy()
    {
        if (spawner != null) spawner.OnEnemySpawned -= TrackSpawned;
        if (formation != null) formation.OnEnemySpawned -= TrackSpawned;
    }

    IEnumerator RunWaves()
    {
        if (waves == null || waves.Count == 0) yield break;

        for (int waveIndex = 0; ; waveIndex++)
        {
            int idx = Mathf.Min(waveIndex, waves.Count - 1);
            var wave = waves[idx];

            if (debugLogs)
                UnityEngine.Debug.Log($"[Wave] START Wave {waveIndex} '{wave.name}' phases={wave.phases.Count}");

            if (wave.startDelay > 0f) yield return new WaitForSeconds(wave.startDelay);
            else yield return null;

            foreach (var phase in wave.phases)
            {
                if (phase.startDelay > 0f) yield return new WaitForSeconds(phase.startDelay);
                else yield return null;

                // Arme la route de la phase (appliquée à chaque spawn)
                currentPhaseRoute = phase.route;

                int targetCount = ScaleCount(phase.count, waveIndex);
                float interval = ScaleInterval(phase.spawnInterval, waveIndex);

                if (debugLogs)
                {
                    string routeName = (phase.route != null) ? phase.route.name : "(none)";
                    UnityEngine.Debug.Log(
                        $"[Wave] PHASE '{phase.name}' style={phase.style} " +
                        $"prefab={(phase.enemyPrefab ? phase.enemyPrefab.name : "(keep)")} " +
                        $"target={targetCount} interval={interval:0.###} route={routeName} waitClear={phase.waitForClear}"
                    );
                }

                // init counters
                aliveInPhase = 0;
                spawnedInPhase = 0;
                targetInPhase = targetCount;
                spawningDone = false;

                if (phase.style == SpawnStyle.SingleRandomTop)
                {
                    if (spawner != null && phase.enemyPrefab != null)
                        spawner.enemyPrefab = phase.enemyPrefab;

                    for (int i = 0; i < targetCount; i++)
                    {
                        spawner?.SpawnOneRandomTop();
                        if (interval > 0f) yield return new WaitForSeconds(interval);
                        else yield return null;
                    }

                    spawningDone = true;
                }
                else // FormationGrid
                {
                    if (formation != null)
                    {
                        if (phase.enemyPrefab != null)
                            formation.enemyPrefab = phase.enemyPrefab;

                        int cols = Mathf.Max(1, phase.cols);
                        formation.cols = cols;
                        formation.rows = Mathf.Max(1, Mathf.CeilToInt((float)targetCount / cols));

                        if (phase.overrideFormationSettings)
                        {
                            formation.flyInDuration = phase.flyInDuration;
                            formation.arcSideOffset = phase.arcSideOffset;
                            formation.arcDownAmount = phase.arcDownAmount;
                            formation.spawnStagger = phase.spawnStagger;
                            formation.rowStaggerBonus = phase.rowStaggerBonus;
                        }

                        formation.SpawnFormation(targetCount);

                        yield return WaitUntilOrTimeout(() => spawningDone, phase.spawnTimeout);
                        spawningDone = true;
                    }
                    else
                    {
                        spawningDone = true;
                    }
                }

                if (phase.waitForClear)
                {
                    if (debugLogs)
                        UnityEngine.Debug.Log($"[Wave] WAIT CLEAR '{phase.name}' (spawningDone={spawningDone}, alive={aliveInPhase})");

                    yield return WaitUntilOrTimeout(() => spawningDone && aliveInPhase <= 0, phase.clearTimeout);
                    aliveInPhase = 0;
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, betweenWavesDelay));

            if (!loopForever && waveIndex >= waves.Count - 1)
                yield break;
        }
    }

    IEnumerator WaitUntilOrTimeout(Func<bool> predicate, float timeout)
    {
        float t = 0f;
        timeout = Mathf.Max(0.1f, timeout);

        while (!predicate() && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    void TrackSpawned(GameObject go)
    {
        if (go == null) return;

        var tracker = go.GetComponent<WaveTrackedEnemy>();
        if (tracker == null) tracker = go.AddComponent<WaveTrackedEnemy>();
        tracker.Init(this);

        //  Route appliquée ici : marche pour SingleRandomTop ET FormationGrid
        var follower = go.GetComponent<EnemyRouteFollower>();
        if (follower != null)
        {
            if (currentPhaseRoute != null) follower.ApplyRoute(currentPhaseRoute);
            else follower.ClearRoute();
        }

        aliveInPhase++;
        spawnedInPhase++;

        if (spawnedInPhase >= targetInPhase)
            spawningDone = true;
    }

    public void NotifyEnemyGone(WaveTrackedEnemy enemy)
    {
        aliveInPhase = Mathf.Max(0, aliveInPhase - 1);
    }

    int ScaleCount(int baseCount, int waveIndex)
    {
        float mult = 1f + (countGrowthPerWave * waveIndex);
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * mult));
    }

    float ScaleInterval(float baseInterval, int waveIndex)
    {
        float mult = 1f - (intervalReductionPerWave * waveIndex);
        float val = baseInterval * Mathf.Max(0.1f, mult);
        return Mathf.Max(minSpawnInterval, val);
    }
}
