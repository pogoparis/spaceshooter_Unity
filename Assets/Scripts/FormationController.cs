using System;
using System.Collections;
using UnityEngine;

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
    public float topMargin = 1.2f;
    public float entryOutsideX = 1.2f;
    public float entryYExtra = 0.5f;

    [Header("Fly-in Path")]
    public float flyInDuration = 1.2f;
    public float arcSideOffset = 2.0f;
    public float arcDownAmount = 1.0f;

    [Header("Stagger (WaveManager expects these)")]
    public float spawnStagger = 0.05f;
    public float rowStaggerBonus = 0.00f;

    [Header("Screen Safety")]
    public Camera gameplayCamera;
    public float zPlane = 0f;
    public float edgePadding = 0.15f;
    public bool clampDuringFlyIn = true;

    [Header("Fly-in Safety")]
    public bool disableEnemyScriptDuringFlyIn = true;
    public bool pauseRouteDuringFlyIn = true;

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

    public void SpawnFormation()
    {
        int count = Mathf.Max(0, rows * cols);
        SpawnFormation(count);
    }

    public void SpawnFormation(object arg)
    {
        if (arg is int i) SpawnFormation(i);
        else SpawnFormation();
    }

    public void SpawnFormation(int count)
    {
        if (enemyPrefab == null) return;
        if (count <= 0) return;

        RefreshWorldRect();

        float enemyHalfWidth = GetPrefabHalfWidth(enemyPrefab);
        float enemyHalfHeight = GetPrefabHalfHeight(enemyPrefab);

        int safeCols = Mathf.Max(1, cols);

        float maxRowSize = Mathf.Min(safeCols, count);
        float halfFormationWidth = (maxRowSize - 1f) * colSpacing * 0.5f;

        float centerX = worldRect.center.x;
        float minCenterX = worldRect.xMin + edgePadding + enemyHalfWidth + halfFormationWidth;
        float maxCenterX = worldRect.xMax - edgePadding - enemyHalfWidth - halfFormationWidth;
        centerX = Mathf.Clamp(centerX, minCenterX, maxCenterX);

        float topY = worldRect.yMax - topMargin;

        for (int i = 0; i < count; i++)
        {
            int r = i / safeCols;
            int c = i % safeCols;

            int remaining = count - (r * safeCols);
            int rowSize = Mathf.Min(safeCols, remaining);
            float rowHalf = (rowSize - 1f) * 0.5f;

            Vector3 targetPos = new Vector3(
                centerX + (c - rowHalf) * colSpacing,
                topY - r * rowSpacing,
                zPlane
            );

            targetPos = ClampInsideScreen(targetPos, enemyHalfWidth, enemyHalfHeight);

            float delay = (i * spawnStagger) + (r * rowStaggerBonus);
            StartCoroutine(SpawnOneDelayed(delay, targetPos, enemyHalfWidth, enemyHalfHeight));
        }
    }

    private IEnumerator SpawnOneDelayed(float delay, Vector3 targetPos, float halfW, float halfH)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float spawnX = (targetPos.x < worldRect.center.x) ? (worldRect.xMin - entryOutsideX) : (worldRect.xMax + entryOutsideX);
        Vector3 spawnPos = new Vector3(spawnX, worldRect.yMax + entryYExtra, zPlane);

        Transform parent = enemyParent;
        GameObject e = (parent != null)
            ? Instantiate(enemyPrefab, spawnPos, Quaternion.identity, parent)
            : Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        var routeFollower = e.GetComponent<EnemyRouteFollower>();
        if (pauseRouteDuringFlyIn && routeFollower != null) routeFollower.SetPaused(true);

        var enemy = e.GetComponent<Enemy>();
        if (disableEnemyScriptDuringFlyIn && enemy != null) enemy.enabled = false;

        OnEnemySpawned?.Invoke(e);

        yield return FlyIn(e.transform, spawnPos, targetPos, halfW, halfH);

        if (pauseRouteDuringFlyIn && routeFollower != null)
        {
            routeFollower.RebaseToCurrentPosition();
            routeFollower.SetPaused(false);
        }

        if (disableEnemyScriptDuringFlyIn && enemy != null)
            enemy.enabled = true;
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
            float t01 = Mathf.Clamp01(time / Mathf.Max(0.0001f, flyInDuration));

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
