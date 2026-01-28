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

    [Header("Enemy")]
    public GameObject enemyPrefab;

    [Min(1)]
    public int enemyCount = 5;

    [Header("Path")]
    public PathData pathTemplate;

    [Header("Layout")]
    public LayoutType layoutType = LayoutType.CustomOffsets;
    public List<Vector2> customOffsets = new();

    [Header("Line")]
    public float lineSpacing = 0.6f;

    [Header("Grid")]
    public int gridCols = 5;
    public float gridColSpacing = 0.8f;
    public float gridRowSpacing = 0.8f;

    [Header("Circle")]
    public float circleRadius = 1.2f;

    [Header("Behavior")]
    public FormationBehavior behavior = FormationBehavior.StayInFormation;
    [Min(0f)] public float breakAfterSeconds = 2f;
    [Min(0)] public int breakAtWaypointIndex = 1;

    [Header("Individual Paths After Break")]
    public List<PathData> individualPaths = new();
}
