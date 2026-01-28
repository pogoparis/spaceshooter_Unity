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

    // =====================================================
    // OFFSET LOGIC — SINGLE SOURCE OF TRUTH (PDF)
    // =====================================================
    public Vector2 GetOffset(int index)
    {
        switch (layoutType)
        {
            case LayoutType.CustomOffsets:
                return GetCustomOffset(index);

            case LayoutType.Line:
                return GetLineOffset(index);

            case LayoutType.Grid:
                return GetGridOffset(index);

            case LayoutType.Circle:
                return GetCircleOffset(index);
        }

        return Vector2.zero;
    }

    // ---------------- CUSTOM ----------------
    Vector2 GetCustomOffset(int index)
    {
        if (customOffsets == null || customOffsets.Count == 0)
            return Vector2.zero;

        if (index < 0 || index >= customOffsets.Count)
            return Vector2.zero;

        return customOffsets[index];
    }

    // ---------------- LINE ----------------
    Vector2 GetLineOffset(int index)
    {
        float half = (enemyCount - 1) * 0.5f;
        float x = (index - half) * lineSpacing;
        return new Vector2(x, 0f);
    }

    // ---------------- GRID ----------------
    Vector2 GetGridOffset(int index)
    {
        if (gridCols <= 0) gridCols = 1;

        int col = index % gridCols;
        int row = index / gridCols;

        float x = col * gridColSpacing;
        float y = -row * gridRowSpacing;

        return new Vector2(x, y);
    }

    // ---------------- CIRCLE ----------------
    Vector2 GetCircleOffset(int index)
    {
        if (enemyCount <= 0)
            return Vector2.zero;

        float angle = (index / (float)enemyCount) * Mathf.PI * 2f;
        float x = Mathf.Cos(angle) * circleRadius;
        float y = Mathf.Sin(angle) * circleRadius;

        return new Vector2(x, y);
    }
}
