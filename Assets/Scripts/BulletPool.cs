using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public GameObject bulletPrefab;
    public int initialSize = 30;

    readonly Queue<GameObject> pool = new();

    void Awake()
    {
        for (int i = 0; i < initialSize; i++)
            CreateBullet();
    }

    GameObject CreateBullet()
    {
        var b = Instantiate(bulletPrefab, transform);
        b.SetActive(false);
        pool.Enqueue(b);
        return b;
    }

    public GameObject Get()
    {
        if (pool.Count == 0) CreateBullet();
        var b = pool.Dequeue();
        b.SetActive(true);
        return b;
    }

    public void Return(GameObject b)
    {
        b.SetActive(false);
        pool.Enqueue(b);
    }
}
