using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RacelineOptimizer
{
    public static class BranchDetector
    {
        public class BranchDetectionConfig
        {
            public float SpikeThreshold { get; set; } = 2.0f; // How much thicker to consider a spike
            public float MinSpikeRatio { get; set; } = 0.15f; // Minimum length ratio to consider removal
            public float MaxSpikeRatio { get; set; } = 0.4f; // Maximum length ratio to consider removal
            public int SmoothingWindow { get; set; } = 5; // Window size for width smoothing
            public float DerivativeThreshold { get; set; } = 1.5f; // Threshold for width change rate
        }

        public static (List<Vector2> innerBoundary, List<Vector2> outerBoundary) ProcessBoundaries(
            List<Vector2> innerBoundary, 
            List<Vector2> outerBoundary,
            BranchDetectionConfig config = null)
        {
            config ??= new BranchDetectionConfig();
            
            if (innerBoundary.Count != outerBoundary.Count)
            {
                UnityEngine.Debug.LogWarning("Inner and outer boundary point counts don't match. Skipping branch detection.");
                return (innerBoundary, outerBoundary);
            }

            var widths = CalculateTrackWidths(innerBoundary, outerBoundary);
            var smoothedWidths = SmoothWidths(widths, config.SmoothingWindow);
            var branches = DetectBranches(smoothedWidths, config);
            
            if (branches.Count > 0)
            {
                UnityEngine.Debug.Log($"Detected {branches.Count} potential branches");
                return MitigateBranches(innerBoundary, outerBoundary, branches, smoothedWidths);
            }
            
            return (innerBoundary, outerBoundary);
        }

        private static List<float> CalculateTrackWidths(List<Vector2> innerBoundary, List<Vector2> outerBoundary)
        {
            var widths = new List<float>();
            
            for (int i = 0; i < innerBoundary.Count; i++)
            {
                float distance = Vector2.Distance(innerBoundary[i], outerBoundary[i]);
                widths.Add(distance);
            }
            
            return widths;
        }

        private static List<float> SmoothWidths(List<float> widths, int windowSize)
        {
            var smoothed = new List<float>();
            int halfWindow = windowSize / 2;
            
            for (int i = 0; i < widths.Count; i++)
            {
                float sum = 0;
                int count = 0;
                
                for (int j = Math.Max(0, i - halfWindow); j <= Math.Min(widths.Count - 1, i + halfWindow); j++)
                {
                    sum += widths[j];
                    count++;
                }
                
                smoothed.Add(sum / count);
            }
            
            return smoothed;
        }

        private static List<BranchInfo> DetectBranches(List<float> smoothedWidths, BranchDetectionConfig config)
        {
            var branches = new List<BranchInfo>();
            var derivatives = CalculateDerivatives(smoothedWidths);
            float avgWidth = smoothedWidths.Average();
            
            bool inSpike = false;
            int spikeStart = -1;
            
            for (int i = 1; i < smoothedWidths.Count - 1; i++)
            {
                float currentWidth = smoothedWidths[i];
                float derivative = Math.Abs(derivatives[i]);
                
                // Check if we're entering a potential spike
                if (!inSpike && currentWidth > avgWidth * config.SpikeThreshold && 
                    derivative > config.DerivativeThreshold)
                {
                    inSpike = true;
                    spikeStart = i;
                }
                // Check if we're exiting a spike
                else if (inSpike && currentWidth <= avgWidth * config.SpikeThreshold && 
                         derivative > config.DerivativeThreshold)
                {
                    int spikeEnd = i;
                    int spikeLength = spikeEnd - spikeStart;
                    float spikeRatio = (float)spikeLength / smoothedWidths.Count;
                    
                    // Only consider it a branch if it's within our size constraints
                    if (spikeRatio >= config.MinSpikeRatio && spikeRatio <= config.MaxSpikeRatio)
                    {
                        // Calculate spike characteristics
                        float maxWidth = smoothedWidths.Skip(spikeStart).Take(spikeLength).Max();
                        float baseWidth = (smoothedWidths[Math.Max(0, spikeStart - 5)] + 
                                         smoothedWidths[Math.Min(smoothedWidths.Count - 1, spikeEnd + 5)]) / 2;
                        
                        branches.Add(new BranchInfo
                        {
                            StartIndex = spikeStart,
                            EndIndex = spikeEnd,
                            MaxWidth = maxWidth,
                            BaseWidth = baseWidth,
                            WidthRatio = maxWidth / baseWidth,
                            Length = spikeLength
                        });
                    }
                    
                    inSpike = false;
                }
            }
            
            return branches;
        }

        private static List<float> CalculateDerivatives(List<float> values)
        {
            var derivatives = new List<float>();
            derivatives.Add(0); // First point has no derivative
            
            for (int i = 1; i < values.Count - 1; i++)
            {
                derivatives.Add(values[i + 1] - values[i - 1]); // Central difference
            }
            
            derivatives.Add(0); // Last point has no derivative
            return derivatives;
        }

        private static (List<Vector2> innerBoundary, List<Vector2> outerBoundary) MitigateBranches(
            List<Vector2> innerBoundary, 
            List<Vector2> outerBoundary,
            List<BranchInfo> branches,
            List<float> smoothedWidths)
        {
            var newInner = new List<Vector2>(innerBoundary);
            var newOuter = new List<Vector2>(outerBoundary);
            
            // Process branches from end to start to avoid index shifting
            foreach (var branch in branches.OrderByDescending(b => b.StartIndex))
            {
                UnityEngine.Debug.Log($"Mitigating branch at indices {branch.StartIndex}-{branch.EndIndex}, " +
                                    $"width ratio: {branch.WidthRatio:F2}, length: {branch.Length}");
                
                MitigateSingleBranch(newInner, newOuter, branch);
            }
            
            return (newInner, newOuter);
        }

        private static void MitigateSingleBranch(
            List<Vector2> innerBoundary, 
            List<Vector2> outerBoundary,
            BranchInfo branch)
        {
            // Strategy: Interpolate through the branch area using the boundary points
            // before and after the branch
            
            int startIdx = Math.Max(0, branch.StartIndex - 2);
            int endIdx = Math.Min(innerBoundary.Count - 1, branch.EndIndex + 2);
            
            Vector2 innerStart = innerBoundary[startIdx];
            Vector2 innerEnd = innerBoundary[endIdx];
            Vector2 outerStart = outerBoundary[startIdx];
            Vector2 outerEnd = outerBoundary[endIdx];
            
            // Calculate interpolation parameters
            int pointsToReplace = endIdx - startIdx - 1;
            
            for (int i = 1; i <= pointsToReplace; i++)
            {
                float t = (float)i / (pointsToReplace + 1);
                
                Vector2 newInner = Vector2.Lerp(innerStart, innerEnd, t);
                Vector2 newOuter = Vector2.Lerp(outerStart, outerEnd, t);
                
                innerBoundary[startIdx + i] = newInner;
                outerBoundary[startIdx + i] = newOuter;
            }
        }

        public static List<float> AnalyzeTrackCharacteristics(List<Vector2> innerBoundary, List<Vector2> outerBoundary)
        {
            var widths = CalculateTrackWidths(innerBoundary, outerBoundary);
            var smoothedWidths = SmoothWidths(widths, 5);
            
            float avgWidth = smoothedWidths.Average();
            float stdDev = (float)Math.Sqrt(smoothedWidths.Select(w => Math.Pow(w - avgWidth, 2)).Average());
            float minWidth = smoothedWidths.Min();
            float maxWidth = smoothedWidths.Max();
            
            UnityEngine.Debug.Log($"Track width analysis - Avg: {avgWidth:F2}, StdDev: {stdDev:F2}, " +
                                $"Min: {minWidth:F2}, Max: {maxWidth:F2}, Range: {maxWidth/minWidth:F2}x");
            
            return smoothedWidths;
        }

        private class BranchInfo
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public float MaxWidth { get; set; }
            public float BaseWidth { get; set; }
            public float WidthRatio { get; set; }
            public int Length { get; set; }
        }
    }

    // Extension methods for easier integration
    public static class EdgeDataExtensions
    {
        public static void ProcessBranches(this EdgeData edgeData, BranchDetector.BranchDetectionConfig config = null)
        {
            var (newInner, newOuter) = BranchDetector.ProcessBoundaries(
                edgeData.InnerBoundary, 
                edgeData.OuterBoundary, 
                config);
            
            edgeData.setNewBoundaries(newInner, newOuter);
        }
    }
}