using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

public static class RacelineAnalyzer
{
    private static List<Vector2> cachedRaceline;
    private static RacelineQuadTree quadTree;
    
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
        
        private const int MAX_POINTS_PER_NODE = 8;
        private const int MAX_DEPTH = 6;
        
        private Rect bounds;
        private List<RacelinePoint> points;
        private RacelineQuadTree[] children;
        private int depth;
        
        public RacelineQuadTree(Rect bounds, int depth = 0)
        {
            this.bounds = bounds;
            this.depth = depth;
            this.points = new List<RacelinePoint>();
            this.children = null;
        }
        
        public void Insert(Vector2 position, int index)
        {
            if (!bounds.Contains(position))
                return;
                
            if (points.Count < MAX_POINTS_PER_NODE || depth >= MAX_DEPTH)
            {
                points.Add(new RacelinePoint(position, index));
                return;
            }
            
            if (children == null)
            {
                Subdivide();
            }
            
            foreach (var child in children)
            {
                child.Insert(position, index);
            }
        }
        
        private void Subdivide()
        {
            float halfWidth = bounds.width / 2f;
            float halfHeight = bounds.height / 2f;
            float x = bounds.x;
            float y = bounds.y;
            
            children = new RacelineQuadTree[4];
            children[0] = new RacelineQuadTree(new Rect(x, y, halfWidth, halfHeight), depth + 1);
            children[1] = new RacelineQuadTree(new Rect(x + halfWidth, y, halfWidth, halfHeight), depth + 1);
            children[2] = new RacelineQuadTree(new Rect(x, y + halfHeight, halfWidth, halfHeight), depth + 1);
            children[3] = new RacelineQuadTree(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight), depth + 1);
            
            // Redistribute existing points to children
            foreach (var point in points)
            {
                foreach (var child in children)
                {
                    child.Insert(point.position, point.index);
                }
            }
            
            points.Clear();
        }
        
        public void FindClosestPoint(Vector2 queryPoint, ref int closestIndex, ref float closestDistance)
        {
            // Check points in this node
            foreach (var point in points)
            {
                float distance = Vector2.Distance(queryPoint, point.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = point.index;
                }
            }
            
            // If we have children, check them in order of proximity
            if (children != null)
            {
                // Create array of children with their distances to query point
                var childDistances = new (RacelineQuadTree child, float distance)[4];
                for (int i = 0; i < 4; i++)
                {
                    childDistances[i] = (children[i], DistanceToRect(queryPoint, children[i].bounds));
                }
                
                // Sort by distance to bounds
                System.Array.Sort(childDistances, (a, b) => a.distance.CompareTo(b.distance));
                
                // Check children in order, pruning those that can't contain closer points
                foreach (var (child, distanceToBounds) in childDistances)
                {
                    if (distanceToBounds < closestDistance)
                    {
                        child.FindClosestPoint(queryPoint, ref closestIndex, ref closestDistance);
                    }
                }
            }
        }
        
        private float DistanceToRect(Vector2 point, Rect rect)
        {
            float dx = Mathf.Max(0, Mathf.Max(rect.xMin - point.x, point.x - rect.xMax));
            float dy = Mathf.Max(0, Mathf.Max(rect.yMin - point.y, point.y - rect.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
    
    /// <summary>
    /// Initialize the RacelineAnalyzer with the given raceline data.
    /// This creates a spatial partitioning structure for fast nearest point queries.
    /// </summary>
    public static void Initialize(List<Vector2> raceline)
    {
        cachedRaceline = new List<Vector2>(raceline);
        
        if (raceline == null || raceline.Count == 0)
        {
            quadTree = null;
            return;
        }
        
        // Calculate bounds of the raceline
        Vector2 min = raceline[0];
        Vector2 max = raceline[0];
        
        foreach (var point in raceline)
        {
            min.x = Mathf.Min(min.x, point.x);
            min.y = Mathf.Min(min.y, point.y);
            max.x = Mathf.Max(max.x, point.x);
            max.y = Mathf.Max(max.y, point.y);
        }
        
        // Add some padding to bounds
        Vector2 padding = (max - min) * 0.1f;
        min -= padding;
        max += padding;
        
        Rect bounds = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        quadTree = new RacelineQuadTree(bounds);
        
        // Insert all raceline points into the quadtree
        for (int i = 0; i < raceline.Count; i++)
        {
            quadTree.Insert(raceline[i], i);
        }
        
        Debug.Log($"RacelineAnalyzer initialized with {raceline.Count} points in quadtree");
    }
    
    /// <summary>
    /// Find the closest raceline point using the spatial partitioning structure.
    /// Returns the index and distance of the closest point.
    /// </summary>
    private static (int index, float distance) FindClosestRacelinePoint(Vector2 position)
    {
        if (quadTree == null || cachedRaceline == null || cachedRaceline.Count == 0)
        {
            return (-1, float.MaxValue);
        }
        
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        
        quadTree.FindClosestPoint(position, ref closestIndex, ref closestDistance);
        
        return (closestIndex, closestDistance);
    }
    public static void UpdateRacelineDeviation(Vector3 currentPosition, bool showTrajectory, 
                                             LineRenderer trajectoryLineRenderer, out float racelineDeviation, 
                                             out float averageTrajectoryDeviation)
    {
        // Get the optimal raceline from TrackMaster
        List<Vector2> optimalRaceline = GetOptimalRaceline();
        if (optimalRaceline == null || optimalRaceline.Count == 0)
        {
            racelineDeviation = 0f;
            averageTrajectoryDeviation = 0f;
            return;
        }

        // Calculate current position deviation from raceline
        Vector2 currentPos2D = new Vector2(currentPosition.x, currentPosition.z);
        racelineDeviation = CalculateDistanceToRaceline(currentPos2D, optimalRaceline);

        // Calculate average trajectory deviation if trajectory is being shown
        if (showTrajectory && trajectoryLineRenderer != null && trajectoryLineRenderer.positionCount > 0)
        {
            averageTrajectoryDeviation = CalculateAverageTrajectoryDeviation(optimalRaceline, trajectoryLineRenderer);
        }
        else
        {
            averageTrajectoryDeviation = 0f;
        }
    }
    
    public static List<Vector2> GetOptimalRaceline()
    {
        // Access the raceline from TrackMaster using the public method
        return TrackMaster.GetCurrentRaceline();
    }
    
    public static float CalculateDistanceToRaceline(Vector2 position, List<Vector2> raceline)
    {
        if (raceline.Count == 0) return 0f;
        
        float minDistance = float.MaxValue;
        int closestPointIndex = -1;

        Profiler.BeginSample("CalculateDistanceToRaceline.Points");

        // Use optimized quadtree search to find the closest point
        if (quadTree != null && cachedRaceline != null && cachedRaceline == raceline)
        {
            var (closestIndex, closestDistance) = FindClosestRacelinePoint(position);
            if (closestIndex >= 0)
            {
                minDistance = closestDistance;
                closestPointIndex = closestIndex;
            }
        }
        else
        {
            // Fallback to linear search if quadtree is not available or raceline doesn't match
            for (int i = 0; i < raceline.Count; i++)
            {
                float distance = Vector2.Distance(position, raceline[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPointIndex = i;
                }
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("CalculateDistanceToRaceline.Segments");

        // Only check the two line segments adjacent to the closest point
        if (closestPointIndex >= 0 && raceline.Count > 1)
        {
            // Previous segment: from previous point to closest point
            int prevIndex = (closestPointIndex - 1 + raceline.Count) % raceline.Count;
            Vector2 prevLineStart = raceline[prevIndex];
            Vector2 prevLineEnd = raceline[closestPointIndex];
            
            float prevDistanceToSegment = DistanceToLineSegment(position, prevLineStart, prevLineEnd);
            if (prevDistanceToSegment < minDistance)
            {
                minDistance = prevDistanceToSegment;
            }
            
            // Next segment: from closest point to next point
            int nextIndex = (closestPointIndex + 1) % raceline.Count;
            Vector2 nextLineStart = raceline[closestPointIndex];
            Vector2 nextLineEnd = raceline[nextIndex];
            
            float nextDistanceToSegment = DistanceToLineSegment(position, nextLineStart, nextLineEnd);
            if (nextDistanceToSegment < minDistance)
            {
                minDistance = nextDistanceToSegment;
            }
        }

        Profiler.EndSample();

        return minDistance;
    }
    
    public static float CalculateAverageTrajectoryDeviation(List<Vector2> raceline, LineRenderer trajectoryLineRenderer)
    {
        if (trajectoryLineRenderer.positionCount == 0) return 0f;
        
        Vector3[] trajectoryPoints = new Vector3[trajectoryLineRenderer.positionCount];
        trajectoryLineRenderer.GetPositions(trajectoryPoints);
        
        float totalDeviation = 0f;
        int validPoints = 0;
        
        // Calculate deviation for each trajectory point
        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            Vector2 trajPoint2D = new Vector2(trajectoryPoints[i].x, trajectoryPoints[i].z);
            float deviation = CalculateDistanceToRaceline(trajPoint2D, raceline);
            totalDeviation += deviation;
            validPoints++;
        }
        
        return validPoints > 0 ? totalDeviation / validPoints : 0f;
    }
    
    public static float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.0001f) // Line segment is essentially a point
        {
            return Vector2.Distance(point, lineStart);
        }
        
        Vector2 lineDirection = line / lineLength;
        Vector2 pointToStart = point - lineStart;
        
        // Project point onto line
        float projection = Vector2.Dot(pointToStart, lineDirection);
        
        // Clamp projection to line segment
        projection = Mathf.Clamp(projection, 0f, lineLength);
        
        // Find closest point on line segment
        Vector2 closestPoint = lineStart + lineDirection * projection;
        
        return Vector2.Distance(point, closestPoint);
    }
}
