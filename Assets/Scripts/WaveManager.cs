using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveManager : MonoBehaviour
{
    public enum SpawnStyle { SingleRandomTop, FormationGrid }

    [Serializable]
    public class WavePhase
    {
        public string name = "Phase";
        public SpawnStyle style = SpawnStyle.SingleRandomTop;

        [Header("Prefab (optionnel : si vide, garde celui du spawner/formation)")]
        public GameObject enemyPrefab;

        [Header("Counts")]
        public int count = 10;

        [Header("Timing")]
        public float startDelay = 1.0f;
        public float spawnInterval = 0.25f;

        [Header("Flow")]
        public bool waitForClear = true;

        [Header("Formation (si FormationGrid)")]
        public int cols = 5;
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

    [Header("Difficulty Scaling")]
    public float countGrowthPerWave = 0.15f;
    public float intervalReductionPerWave = 0.08f;
    public float minSpawnInterval = 0.06f;

    int aliveInPhase = 0;
    int spawnedInPhase = 0;
    int targetInPhase = 0;
    bool spawningDone = false;

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (formation == null) formation = FindFirstObjectByType<FormationController>();

        if (spawner != null) spawner.OnEnemySpawned += TrackSpawned;
        if (formation != null) formation.OnEnemySpawned += TrackSpawned;

        StartCoroutine(RunWaves());
    }

    private void OnDestroy()
    {
        if (spawner != null) spawner.OnEnemySpawned -= TrackSpawned;
        if (formation != null) formation.OnEnemySpawned -= TrackSpawned;
    }

    IEnumerator RunWaves()
    {
        if (waves.Count == 0) yield break;

        for (int waveIndex = 0; ; waveIndex++)
        {
            var wave = waves[Mathf.Clamp(waveIndex, 0, waves.Count - 1)];
            if (wave.startDelay > 0) yield return new WaitForSeconds(wave.startDelay);

            foreach (var phase in wave.phases)
            {
                if (phase.startDelay > 0) yield return new WaitForSeconds(phase.startDelay);

                int targetCount = ScaleCount(phase.count, waveIndex);
                float interval = ScaleInterval(phase.spawnInterval, waveIndex);

                // init phase counters
                aliveInPhase = 0;
                spawnedInPhase = 0;
                targetInPhase = targetCount;
                spawningDone = false;

                if (phase.style == SpawnStyle.SingleRandomTop)
                {
                    if (spawner != null && phase.enemyPrefab != null) spawner.enemyPrefab = phase.enemyPrefab;

                    for (int i = 0; i < targetCount; i++)
                    {
                        spawner?.SpawnOneRandomTop();
                        if (interval > 0) yield return new WaitForSeconds(interval);
                    }

                    spawningDone = true;
                }
                else // FormationGrid (fly-in)
                {
                    if (formation != null)
                    {
                        if (phase.enemyPrefab != null) formation.enemyPrefab = phase.enemyPrefab;

                        formation.cols = Mathf.Max(1, phase.cols);
                        formation.rows = Mathf.Max(1, Mathf.CeilToInt((float)targetCount / formation.cols));

                        formation.SpawnFormation(targetCount);

                        // spawningDone passera à true quand on aura reçu targetCount events
                        yield return new WaitUntil(() => spawningDone);
                    }
                    else
                    {
                        spawningDone = true; // évite un soft-lock si pas de formation dans la scène
                    }
                }

                if (phase.waitForClear)
                {
                    yield return new WaitUntil(() => spawningDone && aliveInPhase <= 0);
                }
            }

            // Endless sur la dernière wave configurée (ça boucle)
            // Si tu veux stopper à la fin : ajoute -> if (waveIndex >= waves.Count - 1) yield break;
        }
    }

    void TrackSpawned(GameObject go)
    {
        if (go == null) return;

        var tracker = go.GetComponent<WaveTrackedEnemy>();
        if (tracker == null) tracker = go.AddComponent<WaveTrackedEnemy>();
        tracker.Init(this);

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
