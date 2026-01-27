using UnityEngine;

public class ExplosionFX : MonoBehaviour
{
    [Header("Speed")]
    public float animSpeed = 1.6f;

    [Header("Fade only on last frame")]
    public float fadeOutDuration = 0.12f;

    [Tooltip("Nom exact du sprite de la dernière frame (ex: explosion8)")]
    public string lastSpriteName = "explosion8";

    private Animator anim;
    private SpriteRenderer sr;

    private bool fading;
    private float fadeT;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        fading = false;
        fadeT = 0f;

        if (anim != null) anim.speed = animSpeed;

        if (sr != null)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    void Update()
    {
        if (sr == null) return;

        // Déclenche le fade SEULEMENT quand on est sur la dernière frame
        if (!fading)
        {
            if (sr.sprite != null && sr.sprite.name == lastSpriteName)
            {
                fading = true;
                fadeT = 0f;
            }
            return;
        }

        // Fade-out rapide
        fadeT += Time.deltaTime;
        float a = 1f - Mathf.Clamp01(fadeT / Mathf.Max(0.001f, fadeOutDuration));
        var col = sr.color;
        col.a = a;
        sr.color = col;

        if (fadeT >= fadeOutDuration)
            Destroy(gameObject);
    }
}
