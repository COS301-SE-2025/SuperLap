using System.Collections.Generic;
using UnityEngine;
using RacelineOptimizer;

public class PSOIntegrator
{
    
    static List<System.Numerics.Vector2> ConvertToNumerics(List<Vector2> unityVectors)
    {
        return unityVectors.ConvertAll(v => new System.Numerics.Vector2(v.x, v.y));
    }

    public static List<Vector2> RunPSO(List<Vector2> innerBoundary, List<Vector2> outerBoundary, int numParticles = 100, int iterations = 60000)
    {
        var innerBoundaryNumerics = ConvertToNumerics(innerBoundary);
        var outerBoundaryNumerics = ConvertToNumerics(outerBoundary);

        List<System.Numerics.Vector2> racelinePreConv = PSOInterface.GetRaceline(innerBoundaryNumerics, outerBoundaryNumerics, "track", numParticles, iterations);
        
        List<Vector2> raceline = new List<Vector2>();
        foreach (var point in racelinePreConv)
        {
            raceline.Add(new Vector2(point.X, point.Y));
        }

        return raceline;
    }
}