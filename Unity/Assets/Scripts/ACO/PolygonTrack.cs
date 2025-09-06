using System;
using System.Numerics;

public class PolygonTrack
{
    Polygon outer;
    Polygon inner;

    public PolygonTrack(Vector2[] outerBounds, Vector2[] innerBounds)
    {
        outer = new Polygon(outerBounds);
        inner = new Polygon(innerBounds);
    }

    public bool PointInTrack(Vector2 point)
    {
        bool inOuter = outer.PointInPolygon(point);
        bool inInner = inner.PointInPolygon(point);

        return inOuter && !inInner;
    }
}