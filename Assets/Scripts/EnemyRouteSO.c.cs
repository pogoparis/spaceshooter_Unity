using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Route", fileName = "Route_")]
public class EnemyRouteSO : ScriptableObject
{
    [Header("Space")]
    [Tooltip("If true, each node.offset is interpreted as Viewport coordinates (0..1). You can use values like -0.2 or 1.2 to spawn slightly off-screen.")]
    public bool useViewportPoints = false;

    [Tooltip("If useViewportPoints is false: if true, node.offset is relative to spawn position. If false, node.offset is a world position.")]
    public bool offsetsRelativeToSpawn = true;

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
        [Tooltip("If useViewportPoints: Viewport (0..1). Otherwise: offset or world position (see offsetsRelativeToSpawn).")]
        public Vector2 offset;

        [Min(0.01f)] public float speed;
        [Min(0f)] public float pause;

        [Tooltip("Optional. Used by EnemyRouteFollower to raise callbacks when the node is reached.")]
        public string actionId;
    }

    public List<RouteNode> nodes = new();

    public bool IsValid => nodes != null && nodes.Count >= 2;
}
