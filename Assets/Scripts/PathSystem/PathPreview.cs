using UnityEngine;

public class PathPreview : MonoBehaviour
{
    public PathData path;
    public Camera previewCamera;

    [Range(8, 128)]
    public int steps = 48;

    private void OnDrawGizmos()
    {
        if (path == null)
            return;

        Camera cam = previewCamera != null
            ? previewCamera
            : Camera.main;

        if (cam == null)
            return;

        Gizmos.color = path.previewColor;

        // === COURBE ===
        Vector3 prev = path.Evaluate(0f, cam, 0f);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 curr = path.Evaluate(t, cam, 0f);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        // === WAYPOINTS ===
        for (int i = 0; i < path.waypoints.Count; i++)
        {
            float t = (path.waypoints.Count <= 1)
                ? 0f
                : i / (float)(path.waypoints.Count - 1);

            Vector3 p = path.Evaluate(t, cam, 0f);

            Gizmos.color =
                i == 0 ? Color.green :
                i == path.waypoints.Count - 1 ? Color.red :
                Color.yellow;

            Gizmos.DrawSphere(p, 0.15f);
        }
    }
}
