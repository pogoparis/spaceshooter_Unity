using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathData))]
public class PathDataGizmos : Editor
{
    void OnSceneGUI()
    {
        Handles.color = Color.red;
        Handles.DrawLine(
            new Vector3(-5, 0, 0),
            new Vector3(5, 0, 0)
        );
    }
}