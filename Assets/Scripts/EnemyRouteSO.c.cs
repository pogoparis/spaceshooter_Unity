using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Route", fileName = "Route_")]
public class EnemyRouteSO : ScriptableObject
{
    [Header("Space")]
    public bool offsetsRelativeToSpawn = true; // recommandé

    [Header("Flow")]
    public bool loop = false;

    public EndMode endMode = EndMode.ContinueDefaultMovement;

    public enum EndMode
    {
        ContinueDefaultMovement, // reprend la descente normale
        StopHere,                // reste sur place à la fin
        Despawn                  // disparaît à la fin
    }

    [Serializable]
    public struct RouteNode
    {
        public Vector2 offset;          // offset si relative, sinon position monde (x,y)
        [Min(0.01f)] public float speed; // unités monde/sec
        [Min(0f)] public float pause;    // secondes à l'arrivée sur ce point
    }

    public List<RouteNode> nodes = new();

    public bool IsValid => nodes != null && nodes.Count >= 2;
}
