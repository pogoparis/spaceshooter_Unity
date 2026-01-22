using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 1.0f;
    public float spawnXRange = 4.2f;

    float nextSpawn;
    float topY;

    void Start()
    {
        var cam = Camera.main;
        topY = (cam != null) ? (cam.transform.position.y + cam.orthographicSize + 1.5f) : 10f;
    }

    void Update()
    {
        if (Time.time >= nextSpawn && enemyPrefab != null)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(-spawnXRange, spawnXRange),
                topY,
                0f
            );

            Instantiate(enemyPrefab, pos, Quaternion.identity);
            nextSpawn = Time.time + spawnInterval;
        }
    }
}
