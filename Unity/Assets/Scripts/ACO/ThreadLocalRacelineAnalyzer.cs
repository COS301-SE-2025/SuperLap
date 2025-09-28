using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Thread-local raceline analyzer that maintains its own quadtree and cache
/// to avoid memory contention between worker threads
/// </summary>
public class ThreadLocalRacelineAnalyzer
{
    private List<Vector2> cachedRaceline;
    private RacelineQuadTree quadTree;

    // Quadtree node structure for spatial partitioning
    private class RacelineQuadTree
    {
        private struct RacelinePoint
        {
            public Vector2 position;
            public int index;

            public RacelinePoint(Vector2 pos, int idx)
            {
                position = pos;
                index = idx;
            }
        }

        private List<RacelinePoint> points;
        private Rect bounds;
        private bool isLeaf;
        private RacelineQuadTree[] children;
        private const int maxPointsPerNode = 10;
        private const int maxDepth = 8;
        private int depth;

        public RacelineQuadTree(Rect bounds, int depth = 0)
        {
            this.bounds = bounds;
            this.depth = depth;
            points = new List<RacelinePoint>();
            isLeaf = true;
            children = null;
        }

        public void Insert(Vector2 position, int index)
        {
            if (!bounds.Contains(position)) return;

            if (isLeaf)
            {
                points.Add(new RacelinePoint(position, index));

                if (points.Count > maxPointsPerNode && depth < maxDepth)
                {
                    Subdivide();
                }
            }
            else
            {
                foreach (var child in children)
                {
                    child.Insert(position, index);
                }
            }
        }

        private void Subdivide()
        {
            isLeaf = false;
            children = new RacelineQuadTree[4];

            float halfWidth = bounds.Width / 2;
            float halfHeight = bounds.Height / 2;
            float centerX = bounds.X + halfWidth;
            float centerY = bounds.Y + halfHeight;

            children[0] = new RacelineQuadTree(new Rect(bounds.X, bounds.Y, halfWidth, halfHeight), depth + 1);
            children[1] = new RacelineQuadTree(new Rect(centerX, bounds.Y, halfWidth, halfHeight), depth + 1);
            children[2] = new RacelineQuadTree(new Rect(bounds.X, centerY, halfWidth, halfHeight), depth + 1);
            children[3] = new RacelineQuadTree(new Rect(centerX, centerY, halfWidth, halfHeight), depth + 1);

            foreach (var point in points)
            {
                foreach (var child in children)
                {
                    child.Insert(point.position, point.index);
                }
            }

            points.Clear();
        }

        public (int closestIndex, float distance) FindClosest(Vector2 position)
        {
            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            if (isLeaf)
            {
                foreach (var point in points)
                {
                    float distance = Vector2.Distance(position, point.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = point.index;
                    }
                }
            }
            else
            {
                foreach (var child in children)
                {
                    if (child.bounds.Contains(position) || child.bounds.DistanceTo(position) < closestDistance)
                    {
                        var (childIndex, childDistance) = child.FindClosest(position);
                        if (childDistance < closestDistance)
                        {
                            closestDistance = childDistance;
                            closestIndex = childIndex;
                        }
                    }
                }
            }

            return (closestIndex, closestDistance);
        }
    }

    private class Rect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= X && point.X < X + Width &&
                   point.Y >= Y && point.Y < Y + Height;
        }

        public float DistanceTo(Vector2 point)
        {
            float dx = Math.Max(0, Math.Max(X - point.X, point.X - (X + Width)));
            float dy = Math.Max(0, Math.Max(Y - point.Y, point.Y - (Y + Height)));
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public void InitializeWithRaceline(List<Vector2> raceline)
    {
        if (raceline == null || raceline.Count == 0) return;

        // Create deep copy to ensure thread isolation
        cachedRaceline = new List<Vector2>();
        foreach (var point in raceline)
        {
            cachedRaceline.Add(new Vector2(point.X, point.Y));
        }

        // Calculate bounds for quadtree
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var point in raceline)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        // Add some padding
        float padding = 10f;
        Rect bounds = new Rect(minX - padding, minY - padding, 
                              maxX - minX + 2 * padding, maxY - minY + 2 * padding);

        quadTree = new RacelineQuadTree(bounds);

        // Insert all raceline points into quadtree
        for (int i = 0; i < raceline.Count; i++)
        {
            quadTree.Insert(raceline[i], i);
        }
    }

    public float CalculateDistanceToRaceline(Vector2 position)
    {
        if (cachedRaceline == null || cachedRaceline.Count == 0) return 0f;

        float minDistance = float.MaxValue;
        int closestPointIndex = -1;

        // Use quadtree if available
        if (quadTree != null)
        {
            var (closestIndex, closestDistance) = quadTree.FindClosest(position);
            if (closestIndex >= 0)
            {
                minDistance = closestDistance;
                closestPointIndex = closestIndex;
            }
        }
        else
        {
            // Fallback to linear search
            for (int i = 0; i < cachedRaceline.Count; i++)
            {
                float distance = Vector2.Distance(position, cachedRaceline[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPointIndex = i;
                }
            }
        }

        // Check line segments adjacent to closest point for more accurate distance
        if (closestPointIndex >= 0 && cachedRaceline.Count > 1)
        {
            // Previous segment
            int prevIndex = (closestPointIndex - 1 + cachedRaceline.Count) % cachedRaceline.Count;
            Vector2 prevLineStart = cachedRaceline[prevIndex];
            Vector2 prevLineEnd = cachedRaceline[closestPointIndex];
            float prevDistanceToSegment = DistanceToLineSegment(position, prevLineStart, prevLineEnd);
            if (prevDistanceToSegment < minDistance)
            {
                minDistance = prevDistanceToSegment;
            }

            // Next segment
            int nextIndex = (closestPointIndex + 1) % cachedRaceline.Count;
            Vector2 nextLineStart = cachedRaceline[closestPointIndex];
            Vector2 nextLineEnd = cachedRaceline[nextIndex];
            float nextDistanceToSegment = DistanceToLineSegment(position, nextLineStart, nextLineEnd);
            if (nextDistanceToSegment < minDistance)
            {
                minDistance = nextDistanceToSegment;
            }
        }

        return minDistance;
    }

    private static float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.Length();

        if (lineLength == 0)
            return Vector2.Distance(point, lineStart);

        float t = Math.Max(0, Math.Min(1, Vector2.Dot(point - lineStart, line) / (lineLength * lineLength)));
        Vector2 projection = lineStart + t * line;
        return Vector2.Distance(point, projection);
    }
}