using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class CornerDetector
{
    // Corner Detection Settings
    public static int AngleWindowSize = 6;             // Number of steps used to compute direction change (must be even)
    public static int AngleSmoothWindow = 3;           // Window size for smoothing the angle change signal
    public static float AngleChangeThreshold = 4f;   // Minimum angle change (in degrees) to consider part of a turn
    public static float NetCornerAngleThreshold = 30f; // Total angle (in degrees) needed to classify a corner
    public static float MinStartEndAngleChange = 20f; // Minimum angle change (in degrees) from start to end of corner
    public static int MinCornerSegments = 35;          // Minimum physical length (in meters) for a corner
    public static int MinCornerSegmentsCount = 40;     // Minimum number of data points that make up a corner
    public static int MaxCornerSegmentsCount = 2000; // Maximum number of data points allowed in a corner
    public static int FlatLimit = 10;                  // Max flat/no-turn segments allowed when extending a corner
    public static int CandidateWindowSize = 15;        // Initial window size to detect potential corner regions
    public static float CandidateAngleThreshold = 15f; // Minimum angle sum (in degrees) for candidate region
    public static int MergeGap = 5;                   // Max number of segments between candidates to allow merging
    public static int SmoothingWindow = 5;             // Window size for smoothing the centerline path
        
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
        public float Length { get; }

        public override string ToString() =>
            $"[{StartIndex}-{EndIndex}] {Angle:F1}° {(IsLeftTurn ? "Left" : "Right")} turn " +
            $"{Length:F1}m\nInner: ({InnerStart.X:F1},{InnerStart.Y:F1})→({InnerEnd.X:F1},{InnerEnd.Y:F1})\n" +
            $"Outer: ({OuterStart.X:F1},{OuterStart.Y:F1})→({OuterEnd.X:F1},{OuterEnd.Y:F1})";

        public CornerSegment(Vector2 innerStart, Vector2 innerEnd,
                            Vector2 outerStart, Vector2 outerEnd,
                            float Angle, bool isLeftTurn,
                            int startIndex, int endIndex, float length)
        {
            InnerStart = innerStart;
            InnerEnd = innerEnd;
            OuterStart = outerStart;
            OuterEnd = outerEnd;
            this.Angle = Angle;
            IsLeftTurn = isLeftTurn;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Length = length;
        }
    }

   
}