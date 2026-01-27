using System.Collections;
using UnityEngine;

public class BossPunch : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 30;

    [Header("Visual")]
    public float thickness = 0.35f;
    [Range(0f, 1f)] public float telegraphAlpha = 0.25f;

    [Header("Timing")]
    public float windup = 0.35f;
    public float extend = 0.18f;
    public float hold = 0.18f;
    public float retract = 0.35f;

    SpriteRenderer sr;
    BoxCollider2D box;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
    }

    public void Fire(Vector3 origin, Vector3 target)
    {
        StopAllCoroutines();
        StartCoroutine(Run(origin, target));
    }

    IEnumerator Run(Vector3 origin, Vector3 target)
    {
        if (sr != null)
        {
            var c = sr.color;
            c.a = telegraphAlpha;
            sr.color = c;
        }

        if (box != null) box.enabled = false;

        float dist = Vector3.Distance(origin, target);
        UpdatePose(origin, target, dist);

        yield return new WaitForSeconds(windup);

        // Extend
        float t = 0f;
        while (t < extend)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, extend));
            UpdatePose(origin, target, dist * k);
            yield return null;
        }

        if (sr != null)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        if (box != null) box.enabled = true;

        yield return new WaitForSeconds(hold);

        // Retract
        t = 0f;
        while (t < retract)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, retract));
            UpdatePose(origin, target, dist * k);
            yield return null;
        }

        Destroy(gameObject);
    }

    void UpdatePose(Vector3 origin, Vector3 target, float length)
    {
        Vector3 dir = (target - origin);
        float dist = dir.magnitude;
        if (dist < 0.0001f) dir = Vector3.right;
        else dir /= dist;

        transform.position = origin;
        transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);

        // 1x1 sprite assumed, scale X = length in world units
        transform.localScale = new Vector3(length, thickness, 1f);

        // Put the center of the stretched sprite in-between origin and tip
        transform.position = origin + dir * (length * 0.5f);

        if (box != null)
        {
            box.size = new Vector2(1f, 1f);
            box.offset = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(damage);
    }
}
