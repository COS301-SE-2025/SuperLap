using System;
using System.Numerics;

public class Polygon
{
    private Vector2[] polygon;
    public Vector2[] Data => polygon;
    
    public Polygon(Vector2[] points)
    {
        polygon = points;
    }
    
    // Copy constructor for thread-safe copies
    public Polygon(Polygon other)
    {
        polygon = new Vector2[other.polygon.Length];
        Array.Copy(other.polygon, polygon, other.polygon.Length);
    }
    
    public bool PointInPolygon(Vector2 point)
    {
        int n = polygon.Length;
        bool inside = false;
        
        // Optimized ray casting algorithm avoiding expensive divisions
        int j = n - 1;
        for (int i = 0; i < n; j = i++)
        {
            // Check if point is within the y-range of this edge
            if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y))
            {
                // Calculate intersection using cross-product to avoid division
                float dy = polygon[j].Y - polygon[i].Y;
                if (dy != 0) // Avoid division by zero
                {
                    float dx = polygon[j].X - polygon[i].X;
                    float intersectionX = polygon[i].X + dx * (point.Y - polygon[i].Y) / dy;
                    
                    if (point.X < intersectionX)
                    {
                        inside = !inside;
                    }
                }
            }
        }
        return inside;
    }

    internal float DistanceToPolygonEdge(Vector2 simPosition)
    {
        float minDistance = float.MaxValue;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];
            float distance = DistanceToSegment(simPosition, a, b);
            minDistance = Math.Min(minDistance, distance);
        }

        return minDistance;
    }

    private float DistanceToSegment(Vector2 simPosition, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = simPosition - a;
        float abSquared = Vector2.Dot(ab, ab);
        if (abSquared == 0) return Vector2.Distance(simPosition, a); // a and b are the same point

        float t = Math.Max(0, Math.Min(1, Vector2.Dot(ap, ab) / abSquared));
        Vector2 projection = a + t * ab;
        return Vector2.Distance(simPosition, projection);
    }
}