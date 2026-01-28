using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathData))]
public class PathDataEditor : Editor
{
    const float POINT_SIZE = 0.1f;
    const float LABEL_OFFSET = 0.15f;

    void OnSceneGUI()
    {
        PathData path = (PathData)target;
        if (path == null || path.waypoints == null || path.waypoints.Count < 2)
            return;

        SceneView sv = SceneView.lastActiveSceneView ?? SceneView.sceneViews[0] as SceneView;
        if (sv == null) return;

        Camera cam = sv.camera;
        if (cam == null) return;
        Handles.color = path.previewColor;

        // Draw points + indices
        for (int i = 0; i < path.waypoints.Count; i++)
        {
            Vector3 p = ToWorld(path, path.waypoints[i].position, cam);
            Handles.SphereHandleCap(0, p, Quaternion.identity, POINT_SIZE, EventType.Repaint);
            Handles.Label(p + Vector3.up * LABEL_OFFSET, i.ToString());
        }

        // Draw lines
        for (int i = 0; i < path.waypoints.Count - 1; i++)
        {
            Vector3 a = ToWorld(path, path.waypoints[i].position, cam);
            Vector3 b = ToWorld(path, path.waypoints[i + 1].position, cam);
            Handles.DrawLine(a, b);
        }
    }

    private Vector3 ToWorld(PathData path, Vector2 p, Camera cam)
    {
        if (path.coordSpace == PathData.CoordSpace.World)
            return new Vector3(p.x, p.y, 0f);

        float z = Mathf.Abs(cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(p.x, p.y, z));
    }
}
