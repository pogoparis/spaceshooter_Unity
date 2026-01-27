using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PathSystem/Path Data", fileName = "PATH_New")]
public class PathData : ScriptableObject
{
    public enum PathType { Linear, BezierQuadratic, BezierCubic, Circle, SineWave }
    public enum CoordSpace { World, Viewport }

    [Header("Identity")]
    public string pathName = "New Path";
    public Color previewColor = Color.magenta;

    [Header("Core")]
    public PathType pathType = PathType.Linear;
    public CoordSpace coordSpace = CoordSpace.World;
    [Min(0.01f)] public float totalDuration = 4f;
    public bool isLooping = false;

    [Header("Waypoints (Linear/Bezier)")]
    public List<PathWaypoint> waypoints = new();

    [Header("Circle")]
    public Vector2 circleCenter = new(0.5f, 1.1f);
    public float circleRadius = 0.2f;
    public float circleStartAngleDeg = 180f;
    public float circleEndAngleDeg = 360f;

    [Header("Sine")]
    public float sineAmplitude = 0.1f;
    public float sineFrequency = 2f;
    public bool sineOnX = true;

    [Serializable]
    public struct PathWaypoint
    {
        public Vector2 position;
        [Range(0.1f, 5f)] public float speedMultiplier;
        [Min(0f)] public float pauseDuration;
    }

    // ===================== CORE =====================

    public Vector3 Evaluate(float t01, Camera cam = null, float targetZ = 0f)
    {
        t01 = Mathf.Clamp01(t01);
        cam ??= Camera.main;

        switch (pathType)
        {
            case PathType.Linear:
                return EvaluateLinear(t01, cam, targetZ);

            case PathType.BezierQuadratic:
                return EvaluateBezierQuadratic(t01, cam, targetZ);

            case PathType.BezierCubic:
                return EvaluateBezierCubic(t01, cam, targetZ);

            case PathType.Circle:
                return EvaluateCircle(t01, cam, targetZ);

            case PathType.SineWave:
                return EvaluateSine(t01, cam, targetZ);

            default:
                return Vector3.zero;
        }
    }

    // ===================== PATH TYPES =====================

    private Vector3 EvaluateLinear(float t, Camera cam, float z)
    {
        if (waypoints == null || waypoints.Count == 0)
            return Vector3.zero;

        if (waypoints.Count == 1)
        {
            Vector2 p = ToWorld(waypoints[0].position, cam, z);
            return new Vector3(p.x, p.y, z);
        }

        int segCount = waypoints.Count - 1;
        float scaled = t * segCount;
        int i = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segCount - 1);
        float lt = scaled - i;

        Vector2 a = GetWorldWaypoint(i, cam, z);
        Vector2 b = GetWorldWaypoint(i + 1, cam, z);
        Vector2 pLerp = Vector2.LerpUnclamped(a, b, lt);

        return new Vector3(pLerp.x, pLerp.y, z);
    }

    private Vector3 EvaluateBezierQuadratic(float t, Camera cam, float z)
    {
        if (waypoints == null || waypoints.Count < 3)
            return Vector3.zero;

        Vector2 p0 = GetWorldWaypoint(0, cam, z);
        Vector2 p1 = GetWorldWaypoint(1, cam, z);
        Vector2 p2 = GetWorldWaypoint(2, cam, z);

        Vector2 p = BezierQuadratic(p0, p1, p2, t);
        return new Vector3(p.x, p.y, z);
    }

    private Vector3 EvaluateBezierCubic(float t, Camera cam, float z)
    {
        if (waypoints == null || waypoints.Count < 4)
            return Vector3.zero;

        Vector2 p0 = GetWorldWaypoint(0, cam, z);
        Vector2 p1 = GetWorldWaypoint(1, cam, z);
        Vector2 p2 = GetWorldWaypoint(2, cam, z);
        Vector2 p3 = GetWorldWaypoint(3, cam, z);

        Vector2 p = BezierCubic(p0, p1, p2, p3, t);
        return new Vector3(p.x, p.y, z);
    }

    private Vector3 EvaluateCircle(float t, Camera cam, float z)
    {
        Vector2 center = ToWorld(circleCenter, cam, z);

        float r = circleRadius;
        if (coordSpace == CoordSpace.Viewport)
        {
            Vector2 c0 = ToWorld(circleCenter, cam, z);
            Vector2 c1 = ToWorld(circleCenter + new Vector2(circleRadius, 0f), cam, z);
            r = Vector2.Distance(c0, c1);
        }

        float ang = Mathf.Lerp(circleStartAngleDeg, circleEndAngleDeg, t) * Mathf.Deg2Rad;
        Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;

        return new Vector3(p.x, p.y, z);
    }

    private Vector3 EvaluateSine(float t, Camera cam, float z)
    {
        if (waypoints == null || waypoints.Count < 2)
            return Vector3.zero;

        Vector2 a = GetWorldWaypoint(0, cam, z);
        Vector2 b = GetWorldWaypoint(waypoints.Count - 1, cam, z);
        Vector2 basePos = Vector2.LerpUnclamped(a, b, t);

        float amp = sineAmplitude;
        if (coordSpace == CoordSpace.Viewport)
        {
            Vector2 v0 = ToWorld(new Vector2(0f, 0.5f), cam, z);
            Vector2 v1 = ToWorld(new Vector2(sineAmplitude, 0.5f), cam, z);
            amp = Vector2.Distance(v0, v1);
        }

        float offset = Mathf.Sin(t * sineFrequency * Mathf.PI * 2f) * amp;
        if (sineOnX) basePos.x += offset;
        else basePos.y += offset;

        return new Vector3(basePos.x, basePos.y, z);
    }

    // ===================== HELPERS =====================

    private Vector2 GetWorldWaypoint(int i, Camera cam, float z)
    {
        if (waypoints == null || waypoints.Count == 0)
            return Vector2.zero;

        i = Mathf.Clamp(i, 0, waypoints.Count - 1);
        return ToWorld(waypoints[i].position, cam, z);
    }

    private Vector2 ToWorld(Vector2 p, Camera cam, float zPlane)
    {
        if (coordSpace == CoordSpace.World)
            return p;

        float zDist = Mathf.Abs(zPlane - cam.transform.position.z);
        Vector3 w = cam.ViewportToWorldPoint(new Vector3(p.x, p.y, zDist));
        return new Vector2(w.x, w.y);
    }

    private static Vector2 BezierQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
    }

    private static Vector2 BezierCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        return (uu * u) * p0
             + (3f * uu * t) * p1
             + (3f * u * tt) * p2
             + (tt * t) * p3;
    }
}
