using System.Collections.Generic;
using UnityEngine;

public class Processor3D
{
    public static (List<Vector2>, List<Vector2>) GetNewBoundaries(TrackImageProcessor.ProcessingResults lastResults, int pointCount)
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

        return (innerPoints, outerPoints);
    }
    public static Mesh GenerateOutputMesh(List<Vector2> innerPoints, List<Vector2> outerPoints)
    {
        // Create a mesh from the inner and outer points
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[innerPoints.Count * 2];
        int[] triangles = new int[innerPoints.Count * 6]; // Changed to include all segments including the closing one
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < innerPoints.Count; i++)
        {
            vertices[i] = new Vector3(innerPoints[i].x, 0, innerPoints[i].y);
            vertices[i + innerPoints.Count] = new Vector3(outerPoints[i].x, 0, outerPoints[i].y);
            uv[i] = new Vector2(0, (float)i / (innerPoints.Count - 1));
            uv[i + innerPoints.Count] = new Vector2(1, (float)i / (outerPoints.Count - 1));

            // Create triangles for each segment, including the last one that connects back to the first
            int nextI = (i + 1) % innerPoints.Count; // Use modulo to wrap around to 0 for the last segment
            int baseIndex = i * 6;
            
            // First triangle (corrected winding order for proper normals)
            triangles[baseIndex] = i;
            triangles[baseIndex + 1] = nextI + innerPoints.Count;
            triangles[baseIndex + 2] = i + innerPoints.Count;

            // Second triangle (corrected winding order for proper normals)
            triangles[baseIndex + 3] = i;
            triangles[baseIndex + 4] = nextI;
            triangles[baseIndex + 5] = nextI + innerPoints.Count;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.name = "TrackMesh";

        return mesh;
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
