using System.Collections.Generic;
using System.Numerics;
public static class ACODrivingRecommendationEngine
{
    public static void UpdateDrivingRecommendationsWithAnalyzer(bool enableRecommendations, Vector2 currentPosition, Vector2 currentForward, 
                                                   float currentSpeed, float currentTurnAngle, float throttleInput, 
                                                   float theoreticalTopSpeed, RecommendationConfig config, 
                                                   MotorcyclePhysicsConfig physicsConfig, PolygonTrack track, List<Vector2> optimalRaceline,
                                                   ThreadLocalRacelineAnalyzer analyzer,
                                                   out bool recommendSpeedUp, out bool recommendSlowDown, 
                                                   out bool recommendTurnLeft, out bool recommendTurnRight)
    {
        if (!enableRecommendations)
        {
            SetAllRecommendationIndicators(false, out recommendSpeedUp, out recommendSlowDown, 
                                         out recommendTurnLeft, out recommendTurnRight);
            return;
        }
        
        if (optimalRaceline == null || optimalRaceline.Count == 0)
        {
            SetAllRecommendationIndicators(false, out recommendSpeedUp, out recommendSlowDown, 
                                         out recommendTurnLeft, out recommendTurnRight);
            return;
        }

        // Calculate recommendations for each input type
        RecommendationAnalysis analysis = AnalyzeRecommendationsWithAnalyzer(optimalRaceline, currentPosition, currentForward, 
                                                               currentSpeed, currentTurnAngle, throttleInput, 
                                                               theoreticalTopSpeed, config, physicsConfig, track, analyzer);

        // Update recommendation states
        recommendSpeedUp = analysis.shouldSpeedUp;
        recommendSlowDown = analysis.shouldSlowDown;
        recommendTurnLeft = analysis.shouldTurnLeft;
        recommendTurnRight = analysis.shouldTurnRight;
    }
    
    private static RecommendationAnalysis AnalyzeRecommendationsWithAnalyzer(List<Vector2> raceline, Vector2 currentPosition, 
                                                               Vector2 currentForward, float currentSpeed, 
                                                               float currentTurnAngle, float throttleInput, 
                                                               float theoreticalTopSpeed, RecommendationConfig config, 
                                                               MotorcyclePhysicsConfig physicsConfig, PolygonTrack track,
                                                               ThreadLocalRacelineAnalyzer analyzer)
    {
        // Get baseline score (continue straight)
        float baselineScore = SimulateRecommendationScenarioWithAnalyzer(0f, 0f, raceline, currentPosition, currentForward, 
                                                           currentSpeed, currentTurnAngle, config, physicsConfig, analyzer);
        
        // First, determine the optimal steering direction
        float turnLeftScore = SimulateRecommendationScenarioWithAnalyzer(0f, -config.testInputStrength, raceline, 
                                                           currentPosition, currentForward, currentSpeed, 
                                                           currentTurnAngle, config, physicsConfig, analyzer);
        float turnRightScore = SimulateRecommendationScenarioWithAnalyzer(0f, config.testInputStrength, raceline, 
                                                            currentPosition, currentForward, currentSpeed, 
                                                            currentTurnAngle, config, physicsConfig, analyzer);

        // Calculate steering improvements
        float turnLeftImprovement = baselineScore - turnLeftScore;
        float turnRightImprovement = baselineScore - turnRightScore;
        
        // Determine optimal steering input for speed tests
        float optimalSteeringInput = 0f;
        if (turnLeftImprovement > config.steeringSensitivity && turnLeftImprovement > turnRightImprovement)
        {
            optimalSteeringInput = -config.testInputStrength;
        }
        else if (turnRightImprovement > config.steeringSensitivity && turnRightImprovement > turnLeftImprovement)
        {
            optimalSteeringInput = config.testInputStrength;
        }

        // Use raycast-based track detection for speed recommendations
        float offTrackRatio;
        bool willGoOffTrack = ACOTrajectoryPredictor.CheckIfPathGoesOffTrack(optimalSteeringInput, currentPosition, currentForward, 
                                                                   currentSpeed, currentTurnAngle, throttleInput, 
                                                                   config.trajectoryLength, config.recommendationSteps, 
                                                                   config.offTrackThreshold, physicsConfig, 
                                                                   out offTrackRatio, track);

        bool shouldSpeedUp = false;
        bool shouldSlowDown = false;
        
        if (willGoOffTrack)
        {
            // If we're going to go off track, recommend slowing down
            shouldSlowDown = true;
        }
        else
        {
            // If we're staying on track, recommend speeding up (unless already at high speed)
            if (currentSpeed < theoreticalTopSpeed * config.maxSpeedRatio)
            {
                shouldSpeedUp = true;
            }
        }

        return new RecommendationAnalysis
        {
            shouldSpeedUp = shouldSpeedUp,
            shouldSlowDown = shouldSlowDown,
            shouldTurnLeft = turnLeftImprovement > config.steeringSensitivity,
            shouldTurnRight = turnRightImprovement > config.steeringSensitivity,
            speedUpImprovement = willGoOffTrack ? 0 : 1, // Simple binary improvement indicator
            slowDownImprovement = willGoOffTrack ? 1 : 0,
            turnLeftImprovement = turnLeftImprovement,
            turnRightImprovement = turnRightImprovement
        };
    }
    
    private static void SetAllRecommendationIndicators(bool active, out bool recommendSpeedUp, out bool recommendSlowDown, 
                                                     out bool recommendTurnLeft, out bool recommendTurnRight)
    {
        recommendSpeedUp = active;
        recommendSlowDown = active;
        recommendTurnLeft = active;
        recommendTurnRight = active;
    }
    
    // Version that uses thread-local analyzer instead of static shared one
    private static float SimulateRecommendationScenarioWithAnalyzer(float testThrottle, float testSteering, List<Vector2> raceline, 
                                                      Vector2 currentPosition, Vector2 currentForward, float currentSpeed, 
                                                      float currentTurnAngle, RecommendationConfig config, 
                                                      MotorcyclePhysicsConfig physicsConfig, ThreadLocalRacelineAnalyzer analyzer)
    {
        // Simulation state - start with current state
        Vector2 simPosition = currentPosition;
        Vector2 simForward = currentForward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = config.trajectoryLength/config.recommendationSteps;
        float totalDeviation = 0f;

        // Simulate forward for the specified number of steps
        for (int step = 0; step < config.recommendationSteps; step++)
        {
            // Calculate deviation at this position using thread-local analyzer ONLY
            // NEVER fall back to static analyzer as it causes memory contention
            float deviation = analyzer != null 
                ? analyzer.CalculateDistanceToRaceline(simPosition)
                : 0f; // Use zero deviation if no analyzer (should not happen in normal operation)
            totalDeviation += deviation;

            // Simulate one step forward with test inputs
            ACOTrajectoryPredictor.SimulateOneStepWithInputs(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle,
                                                        deltaTime, testThrottle, testSteering, physicsConfig);
        }
        
        // Return average deviation over the simulation period
        return totalDeviation / config.recommendationSteps;
    }
    
    private struct RecommendationAnalysis
    {
        public bool shouldSpeedUp;
        public bool shouldSlowDown;
        public bool shouldTurnLeft;
        public bool shouldTurnRight;
        public float speedUpImprovement;
        public float slowDownImprovement;
        public float turnLeftImprovement;
        public float turnRightImprovement;
    }
}