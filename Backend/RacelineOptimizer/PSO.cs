//Partical Swarm Optimization algorithm for Raceline Optimization

using System;
using System.Collections.Generic;
using System.Numerics;

namespace RacelineOptimizer
{
    public class PSO
    {

        private static float Clamp(float value, float min, float max)
        {
            return MathF.Max(min, MathF.Min(max, value));
        }

        private static float GetCornerBias(List<CornerDetector.CornerSegment> corners, int index)
        {
            foreach (var corner in corners)
            {
                if (corner.EndIndex < index)
                    continue;

                float severity = MathF.Min(1f, MathF.Abs(corner.Angle) / 90f);
                float t = Math.Clamp((index - corner.StartIndex) / (float)(corner.EndIndex - corner.StartIndex), 0f, 1f);

                // Outer -> inner -> outer
                float baseBias = MathF.Cos(t * MathF.PI);
                float biasOffset = baseBias * 0.5f * severity;

                return corner.IsLeftTurn
                    ? 0.5f - biasOffset
                    : 0.5f + biasOffset;
            }

            return 0.5f;
        }
        
        private float CalculateCorneringCost(List<(Vector2 inner, Vector2 outer)> track, List<CornerDetector.CornerSegment> corners, float[] ratios)
        {
            float cost = 0f;
            for (int i = 0; i < track.Count; i++)
            {
                float idealBias = GetCornerBias(corners, i);
                float ratio = ratios[i];

                if (idealBias > 0.5f)
                {
                    if (ratios[i] > 0.5f)
                    {
                        cost += (1.0f - ratios[i]) * 20;
                    }
                    else
                    {
                        cost += (1.0f - ratios[i]) * 100;
                    }
                }
                else if (idealBias < 0.5f)
                {
                    if (ratios[i] < 0.5f)
                    {
                        cost += ratios[i] * 10;
                    }
                    else
                    {
                        cost += ratios[i] * 1000;
                    }
                }
            }
            return cost;
        }
        private float EvaluateCost(List<(Vector2 inner, Vector2 outer)> track, float[] ratios, List<CornerDetector.CornerSegment> corners)
        {
            List<Vector2> path = new(track.Count);
            for (int i = 0; i < track.Count; i++)
                path.Add(Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]));

            float cost = 0f;
            float corneringCost = CalculateCorneringCost(track, corners, ratios);
        
            return corneringCost;
        }

        public float[] Optimize(List<(Vector2 inner, Vector2 outer)> track, int numParticles = 30, int iterations = 100)
        {
            int dimensions = track.Count;
            Random rand = new();
            var particles = new List<Particle>();
            float[] globalBest = new float[dimensions];
            float globalBestCost = float.MaxValue;

            for (int i = 0; i < numParticles; i++)
            {
                var p = new Particle(dimensions, rand);
                p.BestCost = EvaluateCost(track, p.Position);
                if (p.BestCost < globalBestCost)
                {
                    globalBestCost = p.BestCost;
                    Array.Copy(p.Position, globalBest, dimensions);
                }
                particles.Add(p);
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                foreach (var p in particles)
                {
                    for (int d = 0; d < dimensions; d++)
                    {
                        float inertia = 0.5f * p.Velocity[d];
                        float cognitive = 1.5f * (float)rand.NextDouble() * (p.BestPosition[d] - p.Position[d]);
                        float social = 1.5f * (float)rand.NextDouble() * (globalBest[d] - p.Position[d]);

                        p.Velocity[d] = inertia + cognitive + social;
                        p.Position[d] += p.Velocity[d];
                        p.Position[d] = Math.Clamp(p.Position[d], 0f, 1f);
                    }

                    float cost = EvaluateCost(track, p.Position);
                    if (cost < p.BestCost)
                    {
                        p.BestCost = cost;
                        Array.Copy(p.Position, p.BestPosition, dimensions);

                        if (cost < globalBestCost)
                        {
                            globalBestCost = cost;
                            Array.Copy(p.Position, globalBest, dimensions);
                        }
                    }
                }
            }

            return globalBest;
        }

        public List<Vector2> SmoothRaceline(List<Vector2> raceline, int iterations = 2)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector2> smoothed = new();
                int count = raceline.Count;

                for (int i = 0; i < count; i++)
                {
                    Vector2 p0 = raceline[i];
                    Vector2 p1 = raceline[(i + 1) % count];
                    smoothed.Add(Vector2.Lerp(p0, p1, 0.25f));
                    smoothed.Add(Vector2.Lerp(p0, p1, 0.75f));
                }

                raceline = smoothed;
            }

            return raceline;
        }

        public List<Vector2> ClampToTrack(List<(Vector2 inner, Vector2 outer)> track, List<Vector2> raceline)
        {
            List<Vector2> clamped = new();

            for (int i = 0; i < track.Count && i < raceline.Count; i++)
            {
                Vector2 A = track[i].inner;
                Vector2 B = track[i].outer;
                Vector2 P = raceline[i];

                // Check if P is between A and B (within track width at this point)
                Vector2 AB = B - A;
                Vector2 AP = P - A;

                float proj = Vector2.Dot(AP, AB) / Vector2.Dot(AB, AB);

                // Distance from point to line segment
                float t = Clamp(proj, 0f, 1f);
                Vector2 projection = Vector2.Lerp(A, B, t);

                // Check if point is "off track"
                float distToSegmentSq = (P - projection).LengthSquared();
                float trackWidthSq = (B - A).LengthSquared();

                // If outside narrow margin from segment line, clamp
                if (proj < 0f || proj > 1f || distToSegmentSq > 1e-4f * trackWidthSq)
                {
                    clamped.Add(projection);
                }
                else
                {
                    clamped.Add(P); // Keep original if already on track
                }
            }

            return clamped;
        }
        
        public List<Vector2> GenerateRaceline(List<(Vector2 inner, Vector2 outer)> track, float[] ratios)
        {
            var raceline = new List<Vector2>();
            for (int i = 0; i < track.Count; i++)
            {
                raceline.Add(Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]));
            }
            return raceline;
        }
    }
}