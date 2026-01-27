using System;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public event Action<GameObject> OnEnemySpawned;

    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Auto Spawn (désactive si WaveManager)")]
    public bool autoSpawn = false;
    public float spawnInterval = 1.0f;

    [Header("Spawn Area")]
    public float spawnXRange = 4.2f;
    public float spawnOutsideTop = 1.5f;

    float nextSpawn;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        nextSpawn = Time.time + spawnInterval;
    }

    void Update()
    {
        if (!autoSpawn) return;

        if (Time.time >= nextSpawn)
        {
            SpawnOneRandomTop();
            nextSpawn = Time.time + Mathf.Max(0.01f, spawnInterval);
        }
    }

    public void SpawnOneRandomTop()
    {
        if (enemyPrefab == null) return;

        float topY = GetTopY();
        Vector3 pos = new Vector3(UnityEngine.Random.Range(-spawnXRange, spawnXRange), topY, 0f);

        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        OnEnemySpawned?.Invoke(go);
    }

    float GetTopY()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return 10f;
        return cam.transform.position.y + cam.orthographicSize + spawnOutsideTop;
    }
}
