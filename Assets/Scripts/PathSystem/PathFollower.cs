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
        Vector3 basePos = currentPath.Evaluate(
            pathProgress01,
            cam,
            transform.position.z
        );

        Vector3 finalPos = basePos;

        if (!hasBroken && formationData != null)
        {
            finalPos += (Vector3)formationOffset;
        }

        transform.position = finalPos;
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
}
