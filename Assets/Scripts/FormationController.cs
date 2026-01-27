using System.Collections;
using UnityEngine;
using System;


public class FormationController : MonoBehaviour
{
    public event Action<GameObject> OnEnemySpawned;

    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;
    public Transform enemyParent;

    [Header("Grid")]
    public int cols = 5;
    public int rows = 4;
    public float rowSpacing = 0.9f;
    public float colSpacing = 1.2f;

    [Header("Placement")]
    public float topMargin = 1.2f;      // marge depuis le haut de l'écran (world units)
    public float entryOutsideX = 1.2f;  // spawn hors écran (world units)
    public float entryYExtra = 0.5f;    // spawn un peu au-dessus (world units)

    [Header("Fly-in Path")]
    public float flyInDuration = 1.2f;
    public float arcSideOffset = 2.0f;
    public float arcDownAmount = 1.0f;

    //  Ces 2 champs sont attendus par WaveManager (d’après tes erreurs)
    [Header("Stagger (WaveManager expects these)")]
    public float spawnStagger = 0.05f;      // délai entre ennemis
    public float rowStaggerBonus = 0.00f;   // bonus de délai par rangée

    [Header("Screen Safety (anti hors-écran)")]
    public Camera gameplayCamera;
    public float zPlane = 0f;
    public float edgePadding = 0.15f;
    public bool clampDuringFlyIn = true;

    private Rect worldRect;

    private void Awake()
    {
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        RefreshWorldRect();
    }

    private void OnEnable() => RefreshWorldRect();

    private void RefreshWorldRect()
    {
        if (gameplayCamera == null) return;

        float zDist = Mathf.Abs(zPlane - gameplayCamera.transform.position.z);
        Vector3 bl = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
        Vector3 tr = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, zDist));
        worldRect = Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
    }

    //  Overload “compat” : WaveManager appelle SpawnFormation(x)
    public void SpawnFormation(object _)
    {
        SpawnFormation();
    }

    //  Ta méthode normale
    public void SpawnFormation()
    {
        if (enemyPrefab == null) return;

        RefreshWorldRect();

        float enemyHalfWidth = GetPrefabHalfWidth(enemyPrefab);
        float enemyHalfHeight = GetPrefabHalfHeight(enemyPrefab);

        float formationWidth = (cols - 1) * colSpacing;
        float formationHeight = (rows - 1) * rowSpacing;

        float centerX = worldRect.center.x;
        float topY = worldRect.yMax - topMargin;

        float halfFormation = formationWidth * 0.5f;
        float minCenterX = worldRect.xMin + edgePadding + enemyHalfWidth + halfFormation;
        float maxCenterX = worldRect.xMax - edgePadding - enemyHalfWidth - halfFormation;
        centerX = Mathf.Clamp(centerX, minCenterX, maxCenterX);

        float startX = centerX - halfFormation;
        float startY = topY;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 targetPos = new Vector3(
                    startX + c * colSpacing,
                    startY - r * rowSpacing,
                    zPlane
                );

                targetPos = ClampInsideScreen(targetPos, enemyHalfWidth, enemyHalfHeight);

                float spawnX = (targetPos.x < worldRect.center.x) ? (worldRect.xMin - entryOutsideX) : (worldRect.xMax + entryOutsideX);
                Vector3 spawnPos = new Vector3(spawnX, worldRect.yMax + entryYExtra, zPlane);

                GameObject e = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, enemyParent);
                OnEnemySpawned?.Invoke(e);


                //  Délai "stagger" attendu par WaveManager
                int index = r * cols + c;
                float delay = (index * spawnStagger) + (r * rowStaggerBonus);

                StartCoroutine(DelayedFlyIn(e.transform, spawnPos, targetPos, delay, enemyHalfWidth, enemyHalfHeight, rf));
            }
        }
    }

    private IEnumerator DelayedFlyIn(
      Transform t, Vector3 start, Vector3 end, float delay,
      float halfW, float halfH, EnemyRouteFollower rf)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return FlyIn(t, start, end, halfW, halfH);
        if (rf != null) rf.SetPaused(false); // route ON après le fly-in
    }

    private IEnumerator FlyIn(Transform t, Vector3 start, Vector3 end, float halfW, float halfH)
    {
        float time = 0f;

        float dirToCenter = Mathf.Sign(worldRect.center.x - end.x);
        if (dirToCenter == 0f) dirToCenter = 1f;

        Vector3 p1 = start + new Vector3(dirToCenter * arcSideOffset, -arcDownAmount, 0f);
        Vector3 p2 = end;

        while (time < flyInDuration)
        {
            time += Time.deltaTime;
            float t01 = Mathf.Clamp01(time / flyInDuration);

            Vector3 pos = Bezier2(start, p1, p2, t01);

            if (clampDuringFlyIn)
                pos = ClampInsideScreen(pos, halfW, halfH, allowTopOutside: true);

            t.position = pos;
            yield return null;
        }

        t.position = ClampInsideScreen(end, halfW, halfH);
    }

    private Vector3 ClampInsideScreen(Vector3 p, float halfW, float halfH, bool allowTopOutside = false)
    {
        float minX = worldRect.xMin + edgePadding + halfW;
        float maxX = worldRect.xMax - edgePadding - halfW;

        float minY = worldRect.yMin + edgePadding + halfH;
        float maxY = worldRect.yMax - edgePadding - halfH;

        p.x = Mathf.Clamp(p.x, minX, maxX);

        if (!allowTopOutside) p.y = Mathf.Clamp(p.y, minY, maxY);
        else p.y = Mathf.Max(p.y, minY);

        p.z = zPlane;
        return p;
    }

    private static Vector3 Bezier2(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }

    private static float GetPrefabHalfWidth(GameObject prefab)
    {
        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr.sprite.bounds.extents.x * Mathf.Abs(sr.transform.lossyScale.x);

        return 0.2f;
    }

    private static float GetPrefabHalfHeight(GameObject prefab)
    {
        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr.sprite.bounds.extents.y * Mathf.Abs(sr.transform.lossyScale.y);

        return 0.2f;
    }
}
