using System;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Legacy auto spawn (désactivé quand WaveManager pilote)")]
    public bool autoSpawn = false;
    public float spawnInterval = 1.0f;

    [Header("Spawn Area")]
    public float screenSideMargin = 0.25f; // marge monde gauche/droite
    public float topExtra = 1.5f;          // spawn au-dessus du haut

    public event Action<GameObject> OnEnemySpawned;

    float nextSpawn;
    Camera cam;
    float topY;
    float enemyHalfW = 0.5f;

    void Start()
    {
        cam = Camera.main;
        RecomputeEnemySize();
        RecomputeTopY();
    }

    void Update()
    {
        if (!autoSpawn) return;
        if (Time.time < nextSpawn) return;
        if (enemyPrefab == null) return;

        SpawnOneRandomTop();
        nextSpawn = Time.time + spawnInterval;
    }

    public GameObject SpawnOneRandomTop()
    {
        if (enemyPrefab == null) return null;
        if (cam == null) cam = Camera.main;

        RecomputeEnemySize();
        RecomputeTopY();

        float halfW = cam.orthographicSize * cam.aspect;
        float xRange = Mathf.Max(0.1f, halfW - screenSideMargin - enemyHalfW);

        Vector3 pos = new Vector3(
            UnityEngine.Random.Range(-xRange, xRange),
            topY,
            0f
        );

        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        OnEnemySpawned?.Invoke(go);
        return go;
    }

    void RecomputeTopY()
    {
        topY = (cam != null) ? (cam.transform.position.y + cam.orthographicSize + topExtra) : 10f;
    }

    void RecomputeEnemySize()
    {
        var sr = enemyPrefab != null ? enemyPrefab.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr != null) enemyHalfW = sr.bounds.size.x * 0.5f;
    }
}
