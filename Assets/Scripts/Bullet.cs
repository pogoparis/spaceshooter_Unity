using UnityEngine;

public class Bullet : MonoBehaviour
{

    public int damage = 1;

    [Header("Bullet")]
    public float speed = 14f;
    public float lifeTime = 2.0f;

    // Boost ajouté au tir (ex: quand le joueur descend)
    float speedBoost = 0f;

    BulletPool pool;
    float topY;

    void Awake()
    {
        pool = GetComponentInParent<BulletPool>();
    }

    public void SetSpeedBoost(float boost)
    {
        speedBoost = boost;
    }

    void OnEnable()
    {
        // IMPORTANT avec le pooling : reset à chaque réutilisation
        speedBoost = 0f;

        // Recalcule la limite haute (au cas où caméra/zoom change)
        var cam = Camera.main;
        if (cam != null)
            topY = cam.transform.position.y + cam.orthographicSize + 1f;
        else
            topY = 9999f;

        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime); // sécurité
    }

    void Update()
    {
        float v = speed + speedBoost;
        // sécurité : on évite de trop ralentir si boost négatif (normalement on n'en envoie pas)
        v = Mathf.Max(2f, v);

        transform.position += Vector3.up * v * Time.deltaTime;

        // Despawn dès que c'est hors écran
        if (transform.position.y > topY)
            ReturnToPool();
    }

    void ReturnToPool()
    {
        CancelInvoke();
        if (pool != null) pool.Return(gameObject);
        else gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeHit(damage);
            ReturnToPool();
        }
    }

}
