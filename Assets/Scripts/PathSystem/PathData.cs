using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PathSystem/Path Data", fileName = "PATH_New")]
public class PathData : ScriptableObject
{
    public enum PathType { Linear, BezierQuadratic, BezierCubic, Circle, SineWave }
    public enum CoordSpace { World, Viewport } // Viewport = coords 0..1 (avec débordement possible -0.2 / 1.2)

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

    [Header("Circle (Circle)")]
    public Vector2 circleCenter = new(0.5f, 1.1f);   // si Viewport: 0..1
    public float circleRadius = 0.2f;                // si Viewport: fraction écran
    public float circleStartAngleDeg = 180f;
    public float circleEndAngleDeg = 360f;

    [Header("Sine (SineWave)")]
    public float sineAmplitude = 0.1f;               // si Viewport: fraction écran
    public float sineFrequency = 2f;                 // nb d’oscillations sur le chemin
    public bool sineOnX = true;

    [Serializable]
    public struct PathWaypoint
    {
        public Vector2 position;
        [Range(0.1f, 5f)] public float speedMultiplier; // futur usage
        [Min(0f)] public float pauseDuration;           // futur usage
    }

    public Vector3 Evaluate(float t01, Camera cam = null, float targetZ = 0f)
    {
        t01 = Mathf.Clamp01(t01);
        cam ??= Camera.main;

        Vector2 P(int i) => ToWorld(GetWaypoint(i), cam, targetZ);
        Vector2 ToWorld(Vector2 p, Camera c, float zPlane)
        {
            if (coordSpace == CoordSpace.World) return p;

            // Viewport -> World (plane zPlane)
            float zDist = Mathf.Abs(zPlane - c.transform.position.z);
            Vector3 w = c.ViewportToWorldPoint(new Vector3(p.x, p.y, zDist));
            return new Vector2(w.x, w.y);
        }

        switch (pathType)
        {
            case PathType.Linear:
                {
                    if (waypoints == null || waypoints.Count == 0) return Vector3.zero;
                    if (waypoints.Count == 1)
                    {
                        Vector2 solo = ToWorld(waypoints[0].position, cam, targetZ);
                        return new Vector3(solo.x, solo.y, targetZ);
                    }

                    int segCount = waypoints.Count - 1;
                    float scaled = t01 * segCount;
                    int i = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segCount - 1);
                    float lt = scaled - i;

                    Vector2 a = P(i);
                    Vector2 b = P(i + 1);
                    Vector2 p = Vector2.LerpUnclamped(a, b, lt);
                    return new Vector3(p.x, p.y, targetZ);
                }

            case PathType.BezierQuadratic:
                {
                    // attend exactement 3 points
                    if (waypoints == null || waypoints.Count < 3) return Vector3.zero;
                    Vector2 p0 = P(0);
                    Vector2 p1 = P(1);
                    Vector2 p2 = P(2);
                    Vector2 p = BezierQuadratic(p0, p1, p2, t01);
                    return new Vector3(p.x, p.y, targetZ);
                }

            case PathType.BezierCubic:
                {
                    // attend exactement 4 points
                    if (waypoints == null || waypoints.Count < 4) return Vector3.zero;
                    Vector2 p0 = P(0);
                    Vector2 p1 = P(1);
                    Vector2 p2 = P(2);
                    Vector2 p3 = P(3);
                    Vector2 p = BezierCubic(p0, p1, p2, p3, t01);
                    return new Vector3(p.x, p.y, targetZ);
                }

            case PathType.Circle:
                {
                    Vector2 center = ToWorld(circleCenter, cam, targetZ);

                    // radius: si Viewport, radius = fraction écran -> convert approx en world via viewport delta
                    float r = circleRadius;
                    if (coordSpace == CoordSpace.Viewport)
                    {
                        Vector2 c0 = ToWorld(circleCenter, cam, targetZ);
                        Vector2 c1 = ToWorld(circleCenter + new Vector2(circleRadius, 0f), cam, targetZ);
                        r = Vector2.Distance(c0, c1);
                    }

                    float ang = Mathf.Lerp(circleStartAngleDeg, circleEndAngleDeg, t01) * Mathf.Deg2Rad;
                    Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    return new Vector3(p.x, p.y, targetZ);
                }

            case PathType.SineWave:
                {
                    if (waypoints == null || waypoints.Count < 2) return Vector3.zero;

                    Vector2 a = P(0);
                    Vector2 b = P(waypoints.Count - 1);
                    Vector2 basePos = Vector2.LerpUnclamped(a, b, t01);

                    float amp = sineAmplitude;
                    if (coordSpace == CoordSpace.Viewport)
                    {
                        Vector2 v0 = ToWorld(new Vector2(0f, 0.5f), cam, targetZ);
                        Vector2 v1 = ToWorld(new Vector2(sineAmplitude, 0.5f), cam, targetZ);
                        amp = Vector2.Distance(v0, v1);
                    }

                    float offset = Mathf.Sin(t01 * sineFrequency * Mathf.PI * 2f) * amp;
                    if (sineOnX) basePos.x += offset;
                    else basePos.y += offset;

                    return new Vector3(basePos.x, basePos.y, targetZ);
                }

            default:
                return Vector3.zero;
        }
    }

    private Vector2 GetWaypoint(int i)
    {
        if (waypoints == null || waypoints.Count == 0) return Vector2.zero;
        i = Mathf.Clamp(i, 0, waypoints.Count - 1);
        return waypoints[i].position;
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
        float uuu = uu * u;
        float ttt = tt * t;

        return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
    }
}
