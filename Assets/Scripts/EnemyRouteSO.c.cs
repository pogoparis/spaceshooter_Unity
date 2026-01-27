using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Route", fileName = "Route_")]
public class EnemyRouteSO : ScriptableObject
{
    [Header("Space")]
    [Tooltip("If true: node.offset is added to spawn/world base position. If false: node.offset is treated as a world position (x,y).")]
    public bool offsetsRelativeToSpawn = true;

    [Tooltip("If true: node.offset is interpreted as Viewport coordinates (0..1). Can go outside for off-screen spawns.")]
    public bool useViewportPoints = false;

    [Tooltip("Optional camera override for viewport conversion. If null, Camera.main will be used.")]
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
        [Tooltip("Offset (relative) or World pos (if not relative). If useViewportPoints=true: Viewport point (0..1).")]
        public Vector2 offset;

        [Min(0.01f)]
        public float speed;

        [Min(0f)]
        public float pause;

        public NodeAction action;
        public string actionParam;
    }

    public enum NodeAction
    {
        None,
        Shoot,
        Spawn,
        Custom
    }

    [Header("Nodes")]
    public List<RouteNode> nodes = new();

    public bool IsValid => nodes != null && nodes.Count >= 2;

    public Vector3 ResolvePoint(RouteNode node, Vector3 baseWorldPos, float targetZ, Camera cam)
    {
        if (useViewportPoints)
        {
            cam ??= (viewportCameraOverride != null) ? viewportCameraOverride : Camera.main;
            if (cam == null)
            {
                // fallback: treat as world if no camera
                return new Vector3(node.offset.x, node.offset.y, targetZ);
            }

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
