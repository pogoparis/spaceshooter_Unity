using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Route", fileName = "Route_")]
public class EnemyRouteSO : ScriptableObject
{
    [Header("Space")]
    public bool offsetsRelativeToSpawn = true;

    public bool useViewportPoints = false;
    public Camera viewportCameraOverride;

    [Header("Flow")]
    public bool loop = false;
    public EndMode endMode = EndMode.ContinueDefaultMovement;

    public enum EndMode
    {
        ContinueDefaultMovement,
        StopHere,
        Despawn
    }

    [Serializable]
    public struct RouteNode
    {
        public Vector2 offset;
        [Min(0.01f)] public float speed;
        [Min(0f)] public float pause;
    }

    [Header("Nodes")]
    public List<RouteNode> nodes = new();

    public bool IsValid => nodes != null && nodes.Count >= 2;

    public Vector3 ResolvePoint(RouteNode node, Vector3 baseWorldPos, float targetZ, Camera cam)
    {
        if (useViewportPoints)
        {
            if (cam == null) cam = (viewportCameraOverride != null) ? viewportCameraOverride : Camera.main;
            if (cam == null) return new Vector3(node.offset.x, node.offset.y, targetZ);

            float zDist = Mathf.Abs(targetZ - cam.transform.position.z);
            Vector3 w = cam.ViewportToWorldPoint(new Vector3(node.offset.x, node.offset.y, zDist));
            w.z = targetZ;
            return w;
        }

        if (offsetsRelativeToSpawn)
            return baseWorldPos + new Vector3(node.offset.x, node.offset.y, 0f);

        return new Vector3(node.offset.x, node.offset.y, targetZ);
    }
}
