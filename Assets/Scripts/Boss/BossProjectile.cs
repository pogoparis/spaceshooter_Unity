using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    [Header("Motion")]
    public Vector2 velocity = Vector2.down;
    public bool homing = false;
    [Range(0f, 1f)] public float homingStrength = 0.35f;
    public float maxSpeed = 6f;
    public float rotateDegreesPerSecond = 0f;

    [Header("Life")]
    public float lifeTime = 6f;

    [Header("Optional: scale animation")]
    public bool scaleOverLifetime = false;
    public float startScale = 1f;
    public float endScale = 1.6f;

    Transform target;
    float alive;

    public void Init(Vector2 initialVelocity, Transform homingTarget = null)
    {
        velocity = initialVelocity;
        target = homingTarget;
    }

    public void SetHomingTarget(Transform homingTarget)
    {
        target = homingTarget;
    }

    void OnEnable()
    {
        alive = 0f;
        if (scaleOverLifetime) transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        alive += dt;

        if (homing && target != null)
        {
            Vector2 toTarget = (target.position - transform.position);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector2 desired = toTarget.normalized * Mathf.Max(0.01f, maxSpeed);
                float t = 1f - Mathf.Pow(1f - homingStrength, dt * 60f);
                velocity = Vector2.Lerp(velocity, desired, t);
                float mag = velocity.magnitude;
                if (mag > maxSpeed) velocity = velocity / mag * maxSpeed;
            }
        }

        transform.position += (Vector3)(velocity * dt);

        if (rotateDegreesPerSecond != 0f)
            transform.Rotate(0f, 0f, rotateDegreesPerSecond * dt);

        if (scaleOverLifetime)
        {
            float p = Mathf.Clamp01(alive / Mathf.Max(0.01f, lifeTime));
            float s = Mathf.Lerp(startScale, endScale, p);
            transform.localScale = Vector3.one * s;
        }

        if (alive >= lifeTime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
