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

        HandleBreakLogic();
        AdvanceProgress();
        ApplyMovement();
    }

    private void HandleBreakLogic()
    {
        if (hasBroken || formationData == null) return;

        switch (formationData.behavior)
        {
            case FormationData.FormationBehavior.BreakAfterTime:
                if (aliveTime >= formationData.breakAfterSeconds)
                    DoBreak();
                break;

            case FormationData.FormationBehavior.BreakOnWaypoint:
                float breakT = EstimateWaypointT(formationData.breakAtWaypointIndex, currentPath);
                if (pathProgress01 >= breakT)
                    DoBreak();
                break;
        }
    }

    private void AdvanceProgress()
    {
        float dur = Mathf.Max(0.01f, currentPath.totalDuration);
        pathProgress01 += (Time.deltaTime / dur) * currentSpeedMultiplier;

        if (pathProgress01 < 1f) return;

        if (currentPath.isLooping)
            pathProgress01 %= 1f;
        else
            Destroy(gameObject); // pooling plus tard
    }

    private void ApplyMovement()
    {
        Vector3 pos = currentPath.Evaluate(pathProgress01, cam, transform.position.z);

        if (!hasBroken &&
            formationData != null &&
            formationData.behavior != FormationData.FormationBehavior.IndividualPathsAfterBreak)

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


// =======================
// GIZMOS DEBUG (SAFE)
// =======================
private void OnDrawGizmosSelected()
    {
        if (currentPath == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        float z = transform.position.z;

        // ----- LIGNE DU CHEMIN -----
        Gizmos.color = currentPath.previewColor;

        const int STEPS = 40;
        Vector3 prev = currentPath.Evaluate(0f, cam, z);

        for (int i = 1; i <= STEPS; i++)
        {
            float t = i / (float)STEPS;
            Vector3 p = currentPath.Evaluate(t, cam, z);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // ----- WAYPOINTS & DIRECTION -----
        if (currentPath.waypoints != null && currentPath.waypoints.Count > 0)
        {
            int count = currentPath.waypoints.Count;

            for (int i = 0; i < count; i++)
            {
                float t = (count <= 1) ? 0f : i / (float)(count - 1);
                Vector3 wp = currentPath.Evaluate(t, cam, z);

                // Couleur dégradée (début - fin)
                Gizmos.color = Color.Lerp(Color.green, Color.red, t);

                // Taille légèrement progressive
                float size = Mathf.Lerp(0.06f, 0.11f, t);
                Gizmos.DrawSphere(wp, size);

                // Petit trait vers le prochain point (sens du chemin)
                if (i < count - 1)
                {
                    float tNext = (count <= 1) ? 0f : (i + 1) / (float)(count - 1);
                    Vector3 next = currentPath.Evaluate(tNext, cam, z);
                    Vector3 dir = (next - wp).normalized;

                    Gizmos.DrawLine(wp, wp + dir * 0.25f);
                }
            }
        }

    }
}
