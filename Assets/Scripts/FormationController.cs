using System.Collections;
using UnityEngine;

public class FormationController : MonoBehaviour
{
    [Header("Grid")]
    public int cols = 5;
    public int rows = 4;
    public float rowSpacing = 0.9f;
    public float colSpacing = 1.2f;

    [Header("Placement")]
    public float topMargin = 1.2f;      // marge depuis le top écran
    public float entryOutsideX = 1.2f;  // spawn hors écran
    public float entryYExtra = 0.5f;    // spawn un peu au-dessus

    [Header("Fly-in Path")]
    public float flyInDuration = 1.2f;  // plus grand = plus lent
    public float arcSideOffset = 2.0f;
    public float arcDownAmount = 4.0f;

    [Header("Timing")]
    public float spawnStagger = 0.12f;  // délai entre deux ennemis (queue leu-leu)
    public float rowStaggerBonus = 0.25f; // petite pause entre les rangées

    [Header("Enemy")]
    public GameObject enemyPrefab;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    public void SpawnFormation()
    {
        if (enemyPrefab == null || cam == null) return;

        // place l'ancre de formation (en haut)
        transform.position = new Vector3(0f, GetTopY() - topMargin, 0f);

        StopAllCoroutines();
        StartCoroutine(SpawnFormationRoutine());
    }

    IEnumerator SpawnFormationRoutine()
    {
        int total = cols * rows;

        for (int i = 0; i < total; i++)
        {
            int c = i % cols;
            int r = i / cols;

            Vector3 localSlot = GetLocalSlot(c, r);
            Vector3 worldTarget = transform.TransformPoint(localSlot);

            // Alterne gauche/droite pour l'entrée
            bool fromLeft = (i % 2 == 0);
            Vector3 worldStart = GetEntryPoint(fromLeft);

            // Option "queue" : décale légèrement le start Y pour que ça fasse file indienne
            // (le 2e est un poil derrière le 1er, etc.)
            worldStart.y += -(i * 0.15f);

            // Control point (arc)
            Vector3 control = worldStart;
            control.y -= arcDownAmount;
            control.x = fromLeft ? (worldStart.x + arcSideOffset) : (worldStart.x - arcSideOffset);

            GameObject e = Instantiate(enemyPrefab);
            var member = e.GetComponent<EnemyFormationMember>();
            if (member == null) member = e.AddComponent<EnemyFormationMember>();

            member.FlyIn(worldStart, control, worldTarget, flyInDuration, transform, localSlot);

            // Délai avant le prochain (queue leu-leu)
            float extraRowPause = (c == cols - 1) ? rowStaggerBonus : 0f;
            yield return new WaitForSeconds(spawnStagger + extraRowPause);
        }
    }

    Vector3 GetLocalSlot(int col, int row)
    {
        float width = (cols - 1) * colSpacing;
        float startX = -width * 0.5f;

        float x = startX + col * colSpacing;
        float y = -row * rowSpacing;

        return new Vector3(x, y, 0f);
    }

    Vector3 GetEntryPoint(bool fromLeft)
    {
        float topY = GetTopY();
        float halfW = cam.orthographicSize * cam.aspect;

        float x = fromLeft ? (-halfW - entryOutsideX) : (halfW + entryOutsideX);
        float y = topY + entryYExtra;

        return new Vector3(x, y, 0f);
    }

    float GetTopY()
    {
        return cam.transform.position.y + cam.orthographicSize;
    }
}
