using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManagerV2 : MonoBehaviour
{
    [Header("Waves (ScriptableObjects)")]
    public List<WaveData> waves = new();

    [Header("Refs")]
    public FormationSpawner formationSpawner;

    [Header("Flow")]
    public float timeBetweenWaves = 2f;

    private int currentWaveIndex = 0;

    void Start()
    {
        if (formationSpawner == null)
            formationSpawner = FindFirstObjectByType<FormationSpawner>();

        StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        while (currentWaveIndex < waves.Count)
        {
            WaveData wave = waves[currentWaveIndex];
            yield return StartCoroutine(RunWave(wave));

            currentWaveIndex++;
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    IEnumerator RunWave(WaveData wave)
    {
        float timer = 0f;
        int spawnIndex = 0;

        var spawns = wave.formationSpawns;

        while (timer < wave.totalDuration)
        {
            timer += Time.deltaTime;

            while (spawnIndex < spawns.Count &&
                   timer >= spawns[spawnIndex].spawnTime)
            {
                TriggerFormationSpawn(spawns[spawnIndex]);
                spawnIndex++;
            }

            yield return null;
        }
    }

    private void TriggerFormationSpawn(WaveData.FormationSpawn spawn)
    {
        if (spawn.formation == null || formationSpawner == null)
            return;

        formationSpawner.SpawnFormation(
            spawn.formation,
            spawn.repeatCount,
            spawn.repeatDelay
        );
    }
}
