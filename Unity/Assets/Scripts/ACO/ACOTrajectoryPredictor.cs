using System;
using System.Diagnostics;
using System.Numerics;

public static class ACOTrajectoryPredictor
{
    /// <summary>
    /// Calculates trajectory points for ACO agent using bearing-based physics simulation
    /// This matches the ACOAgent's physics system exactly
    /// </summary>
    /// <param name="trajectoryPoints">Number of points to calculate</param>
    /// <param name="trajectoryLength">Total time length to simulate in seconds</param>
    /// <param name="currentPosition">Current 2D position</param>
    /// <param name="currentBearing">Current bearing angle in degrees</param>
    /// <param name="currentSpeed">Current speed in m/s</param>
    /// <param name="currentTurnAngle">Current turn angle</param>
    /// <param name="throttleInput">Throttle input (-1 to 1)</param>
    /// <param name="steerInput">Steering input (-1 to 1)</param>
    /// <param name="physicsConfig">Physics configuration</param>
    /// <returns>Array of 2D trajectory points</returns>
    public static Vector2[] CalculateTrajectoryPoints(int trajectoryPoints, float trajectoryLength, 
                                                     Vector2 currentPosition, float currentBearing, 
                                                     float currentSpeed, float currentTurnAngle, 
                                                     float throttleInput, float steerInput, 
                                                     MotorcyclePhysicsConfig physicsConfig)
    {
        Vector2[] points = new Vector2[trajectoryPoints];
        
        // Simulation state - start with current state
        Vector2 simPosition = currentPosition;
        float simBearing = currentBearing;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = simPosition;
            SimulateOneStep(ref simPosition, ref simBearing, ref simSpeed, ref simTurnAngle, 
                          deltaTime, throttleInput, steerInput, physicsConfig);
        }

        return points;
    }

    /// <summary>
    /// Simulates one physics step using bearing-based calculation (matches ACOAgent exactly)
    /// </summary>
    public static void SimulateOneStep(ref Vector2 position, ref float bearing, ref float speed, ref float turnAngle,
                                     float dt, float throttleInput, float steerInput, MotorcyclePhysicsConfig physicsConfig)
    {
        // Simulate movement using same physics as ACOAgent.UpdateMovement
        float drivingForce = MotorcyclePhysicsCalculator.CalculateDrivingForce(physicsConfig.enginePower, speed, physicsConfig.maxTractionForce);
        float resistanceForces = MotorcyclePhysicsCalculator.CalculateResistanceForces(speed, physicsConfig.dragCoefficient,
                                                                                      physicsConfig.frontalArea, physicsConfig.rollingResistanceCoefficient, physicsConfig.mass);

        float netForce = (drivingForce * throttleInput) - resistanceForces;

        if (throttleInput < 0)
        {
            netForce = -physicsConfig.brakingForce - resistanceForces;
        }

        float acceleration = netForce / physicsConfig.mass;
        speed += acceleration * dt;
        speed = Math.Max(0, speed);

        // Simulate turning using same physics as ACOAgent.UpdateTurning
        float steeringMultiplier = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(speed, physicsConfig.minSteeringSpeed,
                                                                                         physicsConfig.fullSteeringSpeed, physicsConfig.steeringIntensity);
        turnAngle += steerInput * physicsConfig.turnRate * steeringMultiplier * dt;
        turnAngle *= (float)Math.Pow(physicsConfig.steeringDecay, dt);

        // Update bearing (matches ACOAgent.UpdateTurning)
        bearing += turnAngle * dt;

        // Calculate forward vector from bearing and move (matches ACOAgent.UpdateMovement)
        Vector2 forward = CalculateForwardFromBearing(bearing);
        float distance = speed * dt;
        position += forward * distance * ACOAgent.Scale;
    }

    /// <summary>
    /// Legacy method for backward compatibility with existing forward vector approach
    /// </summary>
    public static void SimulateOneStep(ref Vector2 position, ref Vector2 forward, ref float speed, ref float turnAngle,
                                     float dt, float throttleInput, float steerInput, MotorcyclePhysicsConfig physicsConfig)
    {
        // Convert forward vector to bearing for consistent physics
        float bearing = CalculateBearingFromForward(forward);
        
        // Use the bearing-based simulation
        SimulateOneStep(ref position, ref bearing, ref speed, ref turnAngle, dt, throttleInput, steerInput, physicsConfig);
        
        // Update forward vector from new bearing
        forward = CalculateForwardFromBearing(bearing);
    }

    public static void SimulateOneStepWithInputs(ref Vector2 position, ref Vector2 forward, ref float speed,
                                               ref float turnAngle, float dt, float simThrottleInput, float simSteerInput,
                                               MotorcyclePhysicsConfig physicsConfig)
    {
        SimulateOneStep(ref position, ref forward, ref speed, ref turnAngle, dt, simThrottleInput, simSteerInput, physicsConfig);
    }

    /// <summary>
    /// Bearing-based variant for testing different inputs
    /// </summary>
    public static void SimulateOneStepWithInputs(ref Vector2 position, ref float bearing, ref float speed,
                                               ref float turnAngle, float dt, float simThrottleInput, float simSteerInput,
                                               MotorcyclePhysicsConfig physicsConfig)
    {
        SimulateOneStep(ref position, ref bearing, ref speed, ref turnAngle, dt, simThrottleInput, simSteerInput, physicsConfig);
    }

    /// <summary>
    /// Calculates forward vector from bearing angle (matches ACOAgent.Forward property)
    /// </summary>
    private static Vector2 CalculateForwardFromBearing(float bearing)
    {
        float rad = (bearing - 90.0f) * (float)Math.PI / 180f;
        return new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad));
    }

    /// <summary>
    /// Calculates bearing angle from forward vector
    /// Fixed to be consistent with CalculateForwardFromBearing
    /// </summary>
    private static float CalculateBearingFromForward(Vector2 forward)
    {
        // This should be the inverse of: rad = (bearing - 90) * π/180; forward = (cos(rad), sin(rad))
        // So: bearing = atan2(forward.Y, forward.X) * 180/π + 90
        float angle = (float)Math.Atan2(forward.Y, forward.X) * 180f / (float)Math.PI;
        return angle + 90.0f;
    }
    
    /// <summary>
    /// Checks if a predicted path goes off track using bearing-based simulation
    /// </summary>
    public static bool CheckIfPathGoesOffTrack(float steeringInput, Vector2 currentPosition, Vector2 currentForward, 
                                             float currentSpeed, float currentTurnAngle, float throttleInput, 
                                             float trajectoryLength, int recommendationSteps, float offTrackThreshold, 
                                             MotorcyclePhysicsConfig physicsConfig, out float offTrackRatio, PolygonTrack track,
                                             float maxDistanceOffTrack = 0.0f)
    {
        // Convert forward vector to bearing for accurate simulation
        float currentBearing = CalculateBearingFromForward(currentForward);
        
        // Simulation state - start with current state
        Vector2 simPosition = currentPosition;
        float simBearing = currentBearing;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / recommendationSteps;
        int offTrackPoints = 0;
        int totalCheckPoints = Math.Min(recommendationSteps, 8); // Check first 8 points for performance

        // Simulate forward and check if any predicted positions are off track
        for (int step = 0; step < totalCheckPoints; step++)
        {
            // Simulate one step forward with current throttle and provided steering
            SimulateOneStep(ref simPosition, ref simBearing, ref simSpeed, ref simTurnAngle,
                          deltaTime, throttleInput, steeringInput, physicsConfig);

            // Check if this position is on track
            if (!track.PointInTrack(simPosition))
            {
                // add a check here to see if the point is just slightly off track
                if (track.GetDistanceToTrackEdge(simPosition) > maxDistanceOffTrack)
                {
                    offTrackPoints++;
                }
            }
        }

        
        
        // Consider path as "going off track" if more than the threshold of check points are off track
        offTrackRatio = (float)offTrackPoints / totalCheckPoints;
        bool goingOffTrack = offTrackRatio > offTrackThreshold;
        
        return goingOffTrack;
    }

    /// <summary>
    /// Predicts trajectory and checks track bounds for multiple future points
    /// </summary>
    public static bool PredictTrajectoryOffTrack(Vector2 currentPosition, Vector2 currentForward, 
                                                float currentSpeed, float currentTurnAngle, 
                                                float throttleInput, float steerInput, 
                                                float trajectoryLength, int steps, 
                                                float offTrackThreshold, MotorcyclePhysicsConfig physicsConfig,
                                                PolygonTrack track, out float offTrackRatio)
    {
        return CheckIfPathGoesOffTrack(steerInput, currentPosition, currentForward, currentSpeed, 
                                     currentTurnAngle, throttleInput, trajectoryLength, steps, 
                                     offTrackThreshold, physicsConfig, out offTrackRatio, track);
    }
}