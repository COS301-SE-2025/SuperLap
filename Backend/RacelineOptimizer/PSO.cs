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

        
    }
}