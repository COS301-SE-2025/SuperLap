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

        double p1x = polygon[0].X, p1y = polygon[0].Y;

        for (int i = 1; i <= n; i++)
        {
            double p2x = polygon[i % n].X, p2y = polygon[i % n].Y;

            if (point.Y > Math.Min(p1y, p2y))
            {
                if (point.Y <= Math.Max(p1y, p2y))
                {
                    if (point.X <= Math.Max(p1x, p2x))
                    {
                        if (p1y != p2y)
                        {
                            double xinters = (point.Y - p1y) * (p2x - p1x) / (p2y - p1y) + p1x;
                            if (p1x == p2x || point.X <= xinters)
                                inside = !inside;
                        }
                        else if (p1x == p2x || point.X <= p1x)
                        {
                            inside = !inside;
                        }
                    }
                }
            }
            p1x = p2x;
            p1y = p2y;
        }

        return inside;
    }
}