using UnityEngine;

public class BossShockwave : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 25;

    [Header("Timing")]
    public float telegraphDelay = 1.5f;
    public float lifeTimeAfterGo = 3f;

    [Header("Motion")]
    public float speed = 6f;

    Collider2D col;
    SpriteRenderer sr;
    float t;
    bool active;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        t = 0f;
        active = (telegraphDelay <= 0f);
        if (col != null) col.enabled = active;
        if (sr != null) sr.enabled = true;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        if (!active)
        {
            // Simple blink while telegraphing
            if (sr != null) sr.enabled = ((int)(t * 10f) % 2) == 0;
            if (t >= telegraphDelay)
            {
                active = true;
                if (sr != null) sr.enabled = true;
                if (col != null) col.enabled = true;
                t = 0f;
            }
            return;
        }

        transform.position += Vector3.down * (speed * dt);

        if (t >= lifeTimeAfterGo)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(damage);
    }
}
