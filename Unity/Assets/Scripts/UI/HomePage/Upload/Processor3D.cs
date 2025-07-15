using System.Collections.Generic;
using UnityEngine;

public class Processor3D
{
    public static void GenerateOutputTrack(TrackImageProcessor.ProcessingResults lastResults, int pointCount)
    {
        // get total distances of inner and outer boundaries
        float innerDistance = GetTotalLineDistance(lastResults.innerBoundary);
        float outerDistance = GetTotalLineDistance(lastResults.outerBoundary);
        float innerSplit = innerDistance / pointCount;
        float outerSplit = outerDistance / pointCount;

        // get lists of new points for inner and outer boundaries, using linear interpolation
        List<Vector2> innerPoints = new List<Vector2>();
        List<Vector2> outerPoints = new List<Vector2>();
        for (int i = 0; i < pointCount; i++)
        {
            float innerDistanceSoFar = i * innerSplit;
            float outerDistanceSoFar = i * outerSplit;

            Vector2 innerPoint = GetInterpolatedPoint(lastResults.innerBoundary, innerDistanceSoFar);
            Vector2 outerPoint = GetInterpolatedPoint(lastResults.outerBoundary, outerDistanceSoFar);

            innerPoints.Add(innerPoint);
            outerPoints.Add(outerPoint);
        }
    }

    private static Vector2 GetInterpolatedPoint(List<Vector2> points, float distance)
    {
        if (points == null || points.Count < 2)
            return Vector2.zero;

        float accumulatedDistance = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            float segmentDistance = Vector2.Distance(points[i], points[i + 1]);
            if (accumulatedDistance + segmentDistance >= distance)
            {
                float t = (distance - accumulatedDistance) / segmentDistance;
                return Vector2.Lerp(points[i], points[i + 1], t);
            }
            accumulatedDistance += segmentDistance;
        }
        return points[points.Count - 1]; // return the last point if distance exceeds total length
    }

    private static float GetTotalLineDistance(List<Vector2> points)
    {
        float totalDistance = 0f;
        if (points == null || points.Count < 2)
            return totalDistance;
        for (int i = 0; i < points.Count - 1; i++)
        {
            totalDistance += Vector2.Distance(points[i], points[i + 1]);
        }
        return totalDistance;
    }
}
