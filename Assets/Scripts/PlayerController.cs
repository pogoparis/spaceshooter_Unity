using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 8f;
    public float screenPadding = 0.15f; // marge intérieure (viewport)

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

        // Viewport → World bounds
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(screenPadding, screenPadding, 0f));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f - screenPadding, 1f - screenPadding, 0f));

        minBounds = new Vector2(bl.x, bl.y);
        maxBounds = new Vector2(tr.x, tr.y);
    }

    // -------- INPUT --------

    Vector2 ReadMoveInput()
    {
        Vector2 move = Vector2.zero;

        // Clavier
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
        }

        return Vector2.ClampMagnitude(move, 1f);
    }

    bool WantsToShoot(Vector3 moveDelta)
    {
        // Clavier
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            return true;

        // Souris
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        // Mobile (safe, même si pas encore utilisé)
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
            return true;

        // Tir automatique quand le joueur bouge
        if (moveDelta.sqrMagnitude > 0.0001f)
            return true;

        return false;
    }

    // -------- UPDATE --------

    void Update()
    {
        // ----- MOVE -----
        Vector2 move = ReadMoveInput();
        Vector3 delta = (Vector3)(move * speed * Time.deltaTime);
        transform.position += delta;

        // Clamp écran
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;

        // Vitesse verticale (pour booster les tirs quand on descend)
        float velY = 0f;
        if (Time.deltaTime > 0f)
            velY = (transform.position.y - lastPos.y) / Time.deltaTime;
        lastPos = transform.position;

        // ----- SHOOT -----
        if (Time.time >= nextFireTime && firePoint != null && bulletPool != null)
        {
            if (WantsToShoot(delta))
            {
                GameObject go = bulletPool.Get();
                if (go != null)
                {
                    go.transform.position = firePoint.position;
                    go.transform.rotation = Quaternion.identity;

                    Bullet b = go.GetComponent<Bullet>();
                    if (b != null)
                    {
                        float downBoost = (velY < 0f)
                            ? (-velY * inheritDownFactor)
                            : 0f;

                        b.SetSpeedBoost(downBoost);
                    }

                    nextFireTime = Time.time + fireRate;
                }
            }
        }
    }
}
