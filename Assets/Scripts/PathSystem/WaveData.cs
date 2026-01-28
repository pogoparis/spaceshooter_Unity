using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PathSystem/Wave Data", fileName = "Wave_01")]
public class WaveData : ScriptableObject
{
    [Header("Identity")]
    public int waveNumber = 1;
    public string waveName = "Wave 01";

    [Header("Timing")]
    public float totalDuration = 10f;
    public bool bossWave = false;

    [Header("Spawns")]
    public List<FormationSpawn> formationSpawns = new();

    [Serializable]
    public class FormationSpawn
    {
        public FormationData formation;
        public float spawnTime = 0f;

        [Header("Repeat")]
        public int repeatCount = 1;
        public float repeatDelay = 0f;
    }
}
