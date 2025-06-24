using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class CornerDetector
{
    // Corner detection settings
    private const float MinCornerAngle = 20.0f;    // Total accumulated angle to consider as corner
    private const int MinCornerSegments = 5;       // Minimum consecutive segments to form a corner
    private const float MinCornerLength = 5.0f;    // Minimum length of a corner
    private const float AngleThreshold = 2.0f;     // Threshold to start corner detection
    private const float ContinueThreshold = 2.0f;  // Threshold to continue existing corner
    private const int SmoothingWindow = 3;         // Window size for angle smoothing

    public struct CornerSegment
    {
        public Vector2 InnerStart { get; }
        public Vector2 InnerEnd { get; }
        public Vector2 OuterStart { get; }
        public Vector2 OuterEnd { get; }
        public float Angle { get; }
        public bool IsLeftTurn { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }

        public override string ToString() =>
            $"[Segments {StartIndex}-{EndIndex}] {Angle:F1}° {(IsLeftTurn ? "Left" : "Right")} turn\n" +
            $"Inner: ({InnerStart.X:F1},{InnerStart.Y:F1})→({InnerEnd.X:F1},{InnerEnd.Y:F1})\n" +
            $"Outer: ({OuterStart.X:F1},{OuterStart.Y:F1})→({OuterEnd.X:F1},{OuterEnd.Y:F1})";
            
        public CornerSegment(Vector2 innerStart, Vector2 innerEnd,
                            Vector2 outerStart, Vector2 outerEnd,
                            float angle, bool isLeftTurn,
                            int startIndex, int endIndex)
        {
            InnerStart = innerStart;
            InnerEnd = innerEnd;
            OuterStart = outerStart;
            OuterEnd = outerEnd;
            Angle = angle;
            IsLeftTurn = isLeftTurn;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
    
    public static List<CornerSegment> DetectCorners(List<(Vector2 inner, Vector2 outer)> track)
    {
        var corners = new List<CornerSegment>();
        
        if (track == null || track.Count < 10)
            return corners;
        
        var rawAngles = CalculateRawAngleChanges(track);
        var smoothedAngles = SmoothAngles(rawAngles);
        
        var cornerRegions = FindCornerRegions(smoothedAngles);
        
        foreach (var region in cornerRegions)
        {
            if (IsValidCorner(track, region.start, region.end, region.totalAngle))
            {
                corners.Add(CreateCornerSegment(track, region.start, region.end, region.totalAngle));
            }
        }
        
        return corners;
    }
    
    private static List<float> CalculateRawAngleChanges(List<(Vector2 inner, Vector2 outer)> track)
    {
        var angleChanges = new List<float>();
        
        if (track.Count < 3)
            return angleChanges;
        
        Vector2 prevDirection = GetMidDirection(track, 0, 1);
        
        for (int i = 1; i < track.Count - 1; i++)
        {
            Vector2 currentDirection = GetMidDirection(track, i, i + 1);
            
            float cross = prevDirection.X * currentDirection.Y - prevDirection.Y * currentDirection.X;
            float dot = Vector2.Dot(prevDirection, currentDirection);
            float angleChange = MathF.Atan2(cross, dot) * (180.0f / MathF.PI);
            
            angleChanges.Add(angleChange);
            prevDirection = currentDirection;
        }
        
        return angleChanges;
    }
    
    private static List<float> SmoothAngles(List<float> rawAngles)
    {
        if (rawAngles.Count < SmoothingWindow)
            return rawAngles.ToList();
        
        var smoothed = new List<float>();
        
        for (int i = 0; i < rawAngles.Count; i++)
        {
            float sum = 0;
            int count = 0;
            
            // Average over smoothing window
            for (int j = Math.Max(0, i - SmoothingWindow/2); 
                 j <= Math.Min(rawAngles.Count - 1, i + SmoothingWindow/2); 
                 j++)
            {
                sum += rawAngles[j];
                count++;
            }
            
            smoothed.Add(sum / count);
        }
        
        return smoothed;
    }
    
    private static List<(int start, int end, float totalAngle)> FindCornerRegions(List<float> angles)
    {
        var regions = new List<(int start, int end, float totalAngle)>();
        
        int i = 0;
        while (i < angles.Count)
        {
            if (Math.Abs(angles[i]) < AngleThreshold)
            {
                i++;
                continue;
            }
            
            int cornerStart = i;
            float accumulatedAngle = angles[i];
            float absAccumulatedAngle = Math.Abs(accumulatedAngle);
            bool isLeftTurn = accumulatedAngle < 0;
            
            i++;
            
            // Continue accumulating while in corner
            while (i < angles.Count)
            {
                float currentAngle = angles[i];
                bool currentIsLeft = currentAngle < 0;
                
                bool shouldContinue = false;
                
                if (Math.Abs(currentAngle) >= AngleThreshold && currentIsLeft == isLeftTurn)
                {
                    shouldContinue = true;
                }
                else if (Math.Abs(currentAngle) < AngleThreshold && absAccumulatedAngle >= ContinueThreshold)
                {
                    shouldContinue = true;
                }
                else if (Math.Abs(currentAngle) < AngleThreshold * 2 && currentIsLeft == isLeftTurn && absAccumulatedAngle >= ContinueThreshold)
                {
                    shouldContinue = true;
                }
                
                if (shouldContinue)
                {
                    accumulatedAngle += currentAngle;
                    absAccumulatedAngle = Math.Abs(accumulatedAngle);
                    i++;
                }
                else
                {
                    int straightCount = 0;
                    int lookAhead = i;
                    while (lookAhead < angles.Count && lookAhead < i + 5 && Math.Abs(angles[lookAhead]) < AngleThreshold)
                    {
                        straightCount++;
                        lookAhead++;
                    }
                    
                    if (straightCount >= 3 || Math.Abs(currentAngle) > AngleThreshold)
                    {
                        // End corner here
                        break;
                    }
                    else
                    {
                        accumulatedAngle += currentAngle;
                        absAccumulatedAngle = Math.Abs(accumulatedAngle);
                        i++;
                    }
                }
            }
            
            if (Math.Abs(accumulatedAngle) >= AngleThreshold * 2)
            {
                regions.Add((cornerStart, i - 1, accumulatedAngle));
            }
        }
        
        return regions;
    }
    
    private static Vector2 GetMidDirection(List<(Vector2 inner, Vector2 outer)> track, int from, int to)
    {
        Vector2 fromMid = (track[from].inner + track[from].outer) * 0.5f;
        Vector2 toMid = (track[to].inner + track[to].outer) * 0.5f;
        Vector2 direction = toMid - fromMid;
        
        float length = direction.Length();
        return length > 0.001f ? direction / length : Vector2.UnitX;
    }
    
    private static bool IsValidCorner(List<(Vector2 inner, Vector2 outer)> track, 
                                     int startIndex, int endIndex, float totalAngle)
    {
        // Check minimum angle requirement
        if (Math.Abs(totalAngle) < MinCornerAngle)
            return false;
        
        // Check minimum segment count
        int segmentCount = endIndex - startIndex + 1;
        if (segmentCount < MinCornerSegments)
            return false;
        
        // Check minimum corner length
        float cornerLength = CalculateCornerLength(track, startIndex, endIndex);
        if (cornerLength < MinCornerLength)
            return false;
        
        return true;
    }
    
    private static float CalculateCornerLength(List<(Vector2 inner, Vector2 outer)> track, 
                                             int startIndex, int endIndex)
    {
        float totalLength = 0;
        
        for (int i = startIndex; i < endIndex && i < track.Count - 1; i++)
        {
            Vector2 mid1 = (track[i].inner + track[i].outer) * 0.5f;
            Vector2 mid2 = (track[i + 1].inner + track[i + 1].outer) * 0.5f;
            totalLength += Vector2.Distance(mid1, mid2);
        }
        
        return totalLength;
    }
    
    private static CornerSegment CreateCornerSegment(List<(Vector2 inner, Vector2 outer)> track,
                                                   int startIndex, int endIndex, float totalAngle)
    {
        return new CornerSegment(
            track[startIndex].inner,
            track[endIndex].inner,
            track[startIndex].outer,
            track[endIndex].outer, 
            totalAngle,
            totalAngle > 0, // Positive angle = left turn
            startIndex,
            endIndex
        );
    }
}