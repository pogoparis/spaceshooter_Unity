using System.Collections;
using UnityEngine;

public class FormationController : MonoBehaviour
{
    [Header("Grid")]
    public int cols = 5;
    public int rows = 4;

    [Tooltip("Si true, calcule colSpacing/rowSpacing d'après la taille du sprite ennemi + padding")]
    public bool autoSpacing = true;

    [Tooltip("Marge ajoutée autour d'un ennemi (en unités du monde)")]
    public float spacingPadding = 0.25f;

    [Tooltip("Marge écran gauche/droite (en unités monde)")]
    public float screenSideMargin = 0.25f;

    [Tooltip("Marge écran haut (en unités monde)")]
    public float screenTopMargin = 0.25f;

    [Tooltip("Si autoSpacing=false, ces valeurs sont utilisées")]
    public float rowSpacing = 1.2f;
    public float colSpacing = 1.6f;

    [Header("Placement")]
    public float topMargin = 0.0f;      // marge supplémentaire (en plus du calcul auto)
    public float entryOutsideX = 1.2f;  // spawn hors écran
    public float entryYExtra = 0.5f;    // spawn un peu au-dessus

    [Header("Fly-in Path")]
    public float flyInDuration = 1.2f;  // plus grand = plus lent
    public float arcSideOffset = 2.0f;
    public float arcDownAmount = 4.0f;

    [Header("Timing")]
    public float spawnStagger = 0.12f;     // délai entre deux ennemis (queue leu-leu)
    public float rowStaggerBonus = 0.25f;  // petite pause entre les rangées

    [Header("Enemy")]
    public GameObject enemyPrefab;

    Camera cam;

    // taille ennemi (en unités monde)
    float enemyW = 1f;
    float enemyH = 1f;

    void Awake()
    {
        cam = Camera.main;
    }

    public void SpawnFormation()
    {
        if (enemyPrefab == null || cam == null) return;

        if (autoSpacing)
            ComputeAutoSpacing();

        // Anchor formation : centré caméra + suffisamment bas pour ne pas couper la 1ère rangée
        float topY = GetTopY();
        float anchorY = topY - (screenTopMargin + (enemyH * 0.5f) + topMargin);
        transform.position = new Vector3(cam.transform.position.x, anchorY, 0f);

        StopAllCoroutines();
        StartCoroutine(SpawnFormationRoutine());
    }

    void ComputeAutoSpacing()
    {
        var sr = enemyPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            enemyW = sr.bounds.size.x;
            enemyH = sr.bounds.size.y;
        }
        else
        {
            enemyW = 1f;
            enemyH = 1f;
        }

        // Base spacing = taille + padding
        colSpacing = enemyW + spacingPadding;
        rowSpacing = enemyH + spacingPadding;

        colSpacing = Mathf.Clamp(colSpacing, 0.8f, 3.0f);
        rowSpacing = Mathf.Clamp(rowSpacing, 0.6f, 3.0f);

        // Largeur dispo écran
        float halfW = cam.orthographicSize * cam.aspect;
        float screenW = halfW * 2f;

        // On veut que TOUT le sprite rentre :
        // totalWidth = (cols-1)*colSpacing + enemyW  <=  screenW - 2*screenSideMargin
        float availableW = screenW - (2f * screenSideMargin);
        float neededW = (cols - 1) * colSpacing + enemyW;

        if (neededW > availableW && cols > 1)
        {
            float maxColSpacing = (availableW - enemyW) / (cols - 1);
            colSpacing = Mathf.Max(0.6f, maxColSpacing);
        }
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

            bool fromLeft = (i % 2 == 0);
            Vector3 worldStart = GetEntryPoint(fromLeft);

            // file indienne
            worldStart.y += -(i * 0.15f);

            Vector3 control = worldStart;
            control.y -= arcDownAmount;
            control.x = fromLeft ? (worldStart.x + arcSideOffset) : (worldStart.x - arcSideOffset);

            GameObject e = Instantiate(enemyPrefab);
            var member = e.GetComponent<EnemyFormationMember>();
            if (member == null) member = e.AddComponent<EnemyFormationMember>();

            member.FlyIn(worldStart, control, worldTarget, flyInDuration, transform, localSlot);

            float extraRowPause = (c == cols - 1) ? rowStaggerBonus : 0f;
            yield return new WaitForSeconds(spawnStagger + extraRowPause);
        }
    }

    Vector3 GetLocalSlot(int col, int row)
    {
        // grille centrée sur l’ancre
        float widthCenters = (cols - 1) * colSpacing;
        float startX = -widthCenters * 0.5f;

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
