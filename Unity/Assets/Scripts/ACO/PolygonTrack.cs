using System;
using System.Numerics;

public class PolygonTrack
{
    Polygon outer;
    Polygon inner;

    public Polygon GetOuter => outer;
    public Polygon GetInner => inner;


    public Vector2[] GetOuterData => outer.Data;
    public Vector2[] GetInnerData => inner.Data;

    public PolygonTrack(Vector2[] outerBounds, Vector2[] innerBounds)
    {
        outer = new Polygon(outerBounds);
        inner = new Polygon(innerBounds);
    }
    
    // Copy constructor for thread-safe copies
    public PolygonTrack(PolygonTrack other)
    {
        outer = new Polygon(other.outer);
        inner = new Polygon(other.inner);
    }

    public bool PointInTrack(Vector2 point)
    {
        // Early exit optimization: check outer boundary first since it's typically larger
        // If not in outer, we can immediately return false without checking inner
        bool inOuter = outer.PointInPolygon(point);
        if (!inOuter)
        {
            return false; // Early exit - not in track
        }
        
        // Only check inner if we're in outer
        bool inInner = inner.PointInPolygon(point);
        return !inInner; // In track if in outer but not in inner
    }
}