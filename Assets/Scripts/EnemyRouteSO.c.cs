using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Route", fileName = "Route_")]
public class EnemyRouteSO : ScriptableObject
{
    [Header("Space")]
    [Tooltip("If true: each node offset is added to the enemy spawn position. If false: node offset is treated as a world position (x,y).")]
    public bool offsetsRelativeToSpawn = true;

    [Tooltip("Optional: if true, node.offset is interpreted as Viewport coordinates (0..1). When enabled, offsetsRelativeToSpawn is ignored.")]
    public bool useViewportPoints = false;

    [Tooltip("Optional camera used to resolve viewport points at runtime. If null, Camera.main will be used by helper methods.")]
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
        [Tooltip("Offset (if RelativeToSpawn) or World position (if not). If useViewportPoints=true: Viewport point (0..1, can go outside).")]
        public Vector2 offset;

        [Min(0.01f)]
        [Tooltip("Move speed in world units per second toward this node.")]
        public float speed;

        [Min(0f)]
        [Tooltip("Pause duration (seconds) once this node is reached.")]
        public float pause;

        [Tooltip("Optional extra info for future behaviors (shoot, spawn, etc). Not used by EnemyRouteFollower yet.")]
        public NodeAction action;

        [Tooltip("Optional action parameter (ex: bullet pattern id). Not used yet.")]
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

    public RouteNode GetNode(int i)
    {
        if (nodes == null || nodes.Count == 0) return default;
        i = Mathf.Clamp(i, 0, nodes.Count - 1);
        return nodes[i];
    }

    public Vector3 ResolvePoint(RouteNode node, Vector3 spawnWorldPos, float targetZ, Camera cam)
    {
        if (useViewportPoints)
        {
            cam ??= (viewportCameraOverride != null) ? viewportCameraOverride : Camera.main;
            if (cam == null)
            {
                // Fallback: treat as world if no camera.
                return new Vector3(node.offset.x, node.offset.y, targetZ);
            }

            float zDist = Mathf.Abs(targetZ - cam.transform.position.z);
            Vector3 w = cam.ViewportToWorldPoint(new Vector3(node.offset.x, node.offset.y, zDist));
            w.z = targetZ;
            return w;
        }

        if (offsetsRelativeToSpawn)
            return spawnWorldPos + new Vector3(node.offset.x, node.offset.y, 0f);

        return new Vector3(node.offset.x, node.offset.y, targetZ);
    }

    public Vector3 ResolvePoint(int nodeIndex, Vector3 spawnWorldPos, float targetZ, Camera cam)
    {
        return ResolvePoint(GetNode(nodeIndex), spawnWorldPos, targetZ, cam);
    }

    public void Normalize()
    {
        // Small safety cleanups (optional call from editor tools later)
        if (nodes == null) nodes = new List<RouteNode>();

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n.speed < 0.01f) n.speed = 0.01f;
            if (n.pause < 0f) n.pause = 0f;
            nodes[i] = n;
        }
    }
}
