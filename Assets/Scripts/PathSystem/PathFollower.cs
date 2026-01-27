using UnityEngine;

public class PathFollower : MonoBehaviour
{
    [Header("Runtime (read-only)")]
    [SerializeField] private PathData currentPath;
    [SerializeField] private float pathProgress01;
    [SerializeField] private float currentSpeedMultiplier = 1f;

    [Header("Formation")]
    public Vector2 formationOffset;
    public int formationIndex;

    private Camera cam;
    private float aliveTime;
    private bool hasBroken;

    private FormationData formationData;

    public void Configure(PathData path, FormationData formation, Vector2 offset, int index, Camera camera)
    {
        currentPath = path;
        formationData = formation;
        formationOffset = offset;
        formationIndex = index;
        cam = camera != null ? camera : Camera.main;

        pathProgress01 = 0f;
        aliveTime = 0f;
        hasBroken = false;
        currentSpeedMultiplier = 1f;
    }

    private void Update()
    {
        if (currentPath == null) return;

        aliveTime += Time.deltaTime;

        // Break logic (simple + efficace pour démarrer)
        if (!hasBroken && formationData != null)
        {
            if (formationData.behavior == FormationData.FormationBehavior.BreakAfterTime &&
                aliveTime >= formationData.breakAfterSeconds)
            {
                DoBreak();
            }
            else if (formationData.behavior == FormationData.FormationBehavior.BreakOnWaypoint)
            {
                float breakT = EstimateWaypointT(formationData.breakAtWaypointIndex, currentPath);
                if (pathProgress01 >= breakT) DoBreak();
            }
        }

        float dur = Mathf.Max(0.01f, currentPath.totalDuration);
        pathProgress01 += (Time.deltaTime / dur) * currentSpeedMultiplier;

        if (pathProgress01 >= 1f)
        {
            if (currentPath.isLooping) pathProgress01 %= 1f;
            else
            {
                Destroy(gameObject); // ou pooling plus tard
                return;
            }
        }

        Vector3 pos = currentPath.Evaluate(pathProgress01, cam, transform.position.z);

        // Offset formation (si pas cassé)
        if (!hasBroken && formationData != null && formationData.behavior != FormationData.FormationBehavior.IndividualPathsAfterBreak)
        {
            pos += (Vector3)formationOffset;
        }

        transform.position = pos;
    }

    private void DoBreak()
    {
        hasBroken = true;

        // Option: chacun récupère son propre chemin
        if (formationData != null &&
            formationData.behavior == FormationData.FormationBehavior.IndividualPathsAfterBreak &&
            formationData.individualPaths != null &&
            formationIndex >= 0 &&
            formationIndex < formationData.individualPaths.Count &&
            formationData.individualPaths[formationIndex] != null)
        {
            currentPath = formationData.individualPaths[formationIndex];
            pathProgress01 = 0f;
        }

        formationOffset = Vector2.zero;
    }

    private static float EstimateWaypointT(int waypointIndex, PathData path)
    {
        if (path == null || path.waypoints == null) return 1f;
        int count = path.waypoints.Count;
        if (count <= 1) return 1f;

        waypointIndex = Mathf.Clamp(waypointIndex, 0, count - 1);
        return waypointIndex / (float)(count - 1);
    }
}
