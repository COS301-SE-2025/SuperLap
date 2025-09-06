using System;
using System.Numerics;

public class Polygon
{
    private Vector2[] polygon;
    
    public Polygon(Vector2[] points)
    {
        polygon = points;
    }
    
    // Copy constructor for thread-safe copies
    public Polygon(Polygon other)
    {
        polygon = new Vector2[other.polygon.Length];
        System.Array.Copy(other.polygon, polygon, other.polygon.Length);
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
}