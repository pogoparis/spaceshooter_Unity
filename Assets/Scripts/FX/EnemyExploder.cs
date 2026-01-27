using UnityEngine;

public class EnemyExploder : MonoBehaviour
{
    public GameObject explosionPrefab;

    public void PlayExplosion()
    {
        if (explosionPrefab == null) return;
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
    }
}
