using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PathSystem/Formation Data", fileName = "FORM_New")]
public class FormationData : ScriptableObject
{
    public enum LayoutType { CustomOffsets, Line, Grid, Circle }
    public enum FormationBehavior { StayInFormation, BreakAfterTime, BreakOnWaypoint, IndividualPathsAfterBreak }

    [Header("Identity")]
    public string formationName = "New Formation";

    [Header("Spawn")]
    public GameObject enemyPrefab;
    public int enemyCount = 5;
    [Min(0f)] public float spawnDelayBetweenEnemies = 0.2f;

    [Header("Path")]
    public PathData pathTemplate;

    [Header("Layout")]
    public LayoutType layoutType = LayoutType.CustomOffsets;
    public List<Vector2> customOffsets = new(); // Enemy 0 = leader (0,0) typiquement

    [Header("Line")]
    public float lineSpacing = 0.6f; // world units

    [Header("Grid")]
    public int gridCols = 5;
    public float gridColSpacing = 0.8f;
    public float gridRowSpacing = 0.8f;

    [Header("Circle")]
    public float circleRadius = 1.2f;

    [Header("Behavior")]
    public FormationBehavior behavior = FormationBehavior.StayInFormation;
    [Min(0f)] public float breakAfterSeconds = 2f;
    [Min(0)] public int breakAtWaypointIndex = 1; // basé sur waypoints (Linear/Bezier)

    [Header("Individual Paths After Break")]
    public List<PathData> individualPaths = new(); // optionnel, taille >= enemyCount
}
