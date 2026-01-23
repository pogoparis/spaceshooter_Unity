using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 8f;
    public float screenPadding = 0.15f; // marge intérieure

    [Header("Shoot")]
    public Transform firePoint;
    public BulletPool bulletPool;
    public float fireRate = 0.16f;
    public float inheritDownFactor = 1.0f;

    float nextFireTime;
    Vector3 lastPos;

    Camera cam;
    Vector2 minBounds;
    Vector2 maxBounds;

    void Start()
    {
        cam = Camera.main;
        RecomputeBounds();
        lastPos = transform.position;
    }

    void RecomputeBounds()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Taille du sprite du player en unités
        float padX = screenPadding;
        float padY = screenPadding;

        // Mode arcade : on clamp sur le CENTRE (ignorer la taille du sprite)
        bool clampCenterX = true;
        bool clampCenterY = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (!clampCenterX) padX += sr.bounds.extents.x; // sprite 100% dans l'écran
            if (!clampCenterY) padY += sr.bounds.extents.y; // sprite 100% dans l'écran
        }


        float camX = cam.transform.position.x;
        float camY = cam.transform.position.y;

        minBounds = new Vector2(camX - halfW + padX, camY - halfH + padY);
        maxBounds = new Vector2(camX + halfW - padX, camY + halfH - padY);
    }

    void Update()
    {
        // Move
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 delta = new Vector3(x, y, 0f).normalized * speed * Time.deltaTime;
        transform.position += delta;

        // Clamp écran
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;

        // Vitesse verticale (pour booster uniquement quand tu descends)
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
