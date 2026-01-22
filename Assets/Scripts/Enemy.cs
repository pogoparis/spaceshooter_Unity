using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 3;
    public float speed = 2.5f;

    [Header("HP Bar")]
    public Transform hpFill;              // assigne HPFill ici (drag & drop)
    public bool hideBarWhenFull = true;   // optionnel

    int hp;
    float bottomY;

    // HP bar anchoring
    Vector3 fillBasePos;
    Vector3 fillBaseScale;

    // Hit flash (optionnel)
    SpriteRenderer sr;
    Color baseColor;
    float flashTimer;

    void OnEnable()
    {
        hp = maxHp;

        var cam = Camera.main;
        bottomY = (cam != null) ? (cam.transform.position.y - cam.orthographicSize - 2f) : -9999f;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        flashTimer = 0f;

        if (hpFill != null)
        {
            fillBasePos = hpFill.localPosition;
            fillBaseScale = hpFill.localScale;
            UpdateHpBar();
        }
    }

    void Update()
    {
        // Petit flash à l'impact
        if (sr != null && flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = (flashTimer > 0f) ? Color.white : baseColor;
        }

        // Formation: ne pas bouger pendant l'entrée / ni en formation
        var member = GetComponent<EnemyFormationMember>();
        if (member != null && (member.isFlyingIn || member.inFormation))
            return;

        // Mouvement normal (si pas formation)
        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y < bottomY)
            gameObject.SetActive(false);
    }

    public void TakeHit(int dmg)
    {
        hp -= dmg;
        flashTimer = 0.08f;

        UpdateHpBar();

        if (hp <= 0)
            gameObject.SetActive(false);
    }

    void UpdateHpBar()
    {
        if (hpFill == null) return;

        float ratio = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));

        // Scale X du fill
        Vector3 s = fillBaseScale;
        s.x = fillBaseScale.x * ratio;
        hpFill.localScale = s;

        // Pour que ça "vide" vers la droite (garde le bord gauche fixe)
        // Avec un sprite centré, on décale le fill vers la gauche quand il rétrécit.
        float dx = (fillBaseScale.x - s.x) * 0.5f;
        hpFill.localPosition = new Vector3(fillBasePos.x - dx, fillBasePos.y, fillBasePos.z);

        // Option : cacher quand full
        if (hideBarWhenFull)
            hpFill.parent.gameObject.SetActive(hp < maxHp);
        else
            hpFill.parent.gameObject.SetActive(true);
    }
}
