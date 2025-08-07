using UnityEngine;
using System.Collections.Generic;

public static class RacelineAnalyzer
{
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
        
        // Find the closest point on the raceline
        for (int i = 0; i < raceline.Count; i++)
        {
            float distance = Vector2.Distance(position, raceline[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        // Also check distances to line segments between consecutive points
        for (int i = 0; i < raceline.Count; i++)
        {
            int nextIndex = (i + 1) % raceline.Count;
            Vector2 lineStart = raceline[i];
            Vector2 lineEnd = raceline[nextIndex];
            
            float distanceToSegment = DistanceToLineSegment(position, lineStart, lineEnd);
            if (distanceToSegment < minDistance)
            {
                minDistance = distanceToSegment;
            }
        }
        
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
