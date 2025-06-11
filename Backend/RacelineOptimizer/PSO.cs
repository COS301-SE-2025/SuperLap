//Partical Swarm Optimization algorithm for Raceline Optimization

using System;
using System.Collections.Generic;
using System.Numerics;

namespace RacelineOptimizer
{
    public class PSO
    {
        private float EvaluateCost(List<(Vector2 inner, Vector2 outer)> track, float[] ratios)
        {
            float cost = 0f;
            List<Vector2> path = new();

            for (int i = 0; i < track.Count; i++)
            {
                var point = Vector2.Lerp(track[i].inner, track[i].outer, ratios[i]);
                path.Add(point);
            }

            for (int i = 1; i < path.Count - 1; i++)
            {
                var v1 = Vector2.Normalize(path[i] - path[i - 1]);
                var v2 = Vector2.Normalize(path[i + 1] - path[i]);
                float dot = Vector2.Dot(v1, v2);
                cost += 1f - dot;
            }

            for (int i = 1; i < path.Count; i++)
            {
                cost += Vector2.Distance(path[i], path[i - 1]);
            }

            return cost;
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