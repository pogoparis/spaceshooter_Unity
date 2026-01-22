using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 8f;
    public Vector2 minBounds = new(-4.5f, -7.2f);
    public Vector2 maxBounds = new(4.5f, 7.2f);

    [Header("Shoot")]
    public Transform firePoint;
    public BulletPool bulletPool;
    public float fireRate = 0.16f;           // cadence fixe (auto-fire)
    public float inheritDownFactor = 1.0f;   // boost quand tu descends (0.6 à 1.4 selon feeling)

    float nextFireTime;
    Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        // Move
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 delta = new Vector3(x, y, 0f).normalized * speed * Time.deltaTime;
        transform.position += delta;

        // Clamp
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;

        // Vitesse verticale du player
        float velY = 0f;
        if (Time.deltaTime > 0f)
            velY = (transform.position.y - lastPos.y) / Time.deltaTime;
        lastPos = transform.position;

        // Auto-fire cadence fixe
        if (Time.time >= nextFireTime && firePoint != null && bulletPool != null)
        {
            var go = bulletPool.Get();
            go.transform.position = firePoint.position;
            go.transform.rotation = Quaternion.identity;

            // Boost uniquement quand tu descends (velY négatif)
            var b = go.GetComponent<Bullet>();
            if (b != null)
            {
                float downBoost = (velY < 0f) ? (-velY * inheritDownFactor) : 0f;
                b.SetSpeedBoost(downBoost);
            }

            nextFireTime = Time.time + fireRate;
        }
    }
}
