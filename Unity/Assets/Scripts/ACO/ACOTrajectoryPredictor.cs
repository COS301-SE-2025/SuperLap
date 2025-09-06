using System;
using System.Numerics;

public class ACOTrajectoryPredictor
{
    public static void SimulateOneStep(ref Vector2 position, ref Vector2 forward, ref float speed, ref float turnAngle,
                                     float dt, float throttleInput, float steerInput, MotorcyclePhysicsConfig physicsConfig)
    {
        // Simulate movement using same physics as UpdateMovement
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

        // Simulate turning using same physics as UpdateTurning
        float steeringMultiplier = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(speed, physicsConfig.minSteeringSpeed,
                                                                                         physicsConfig.fullSteeringSpeed, physicsConfig.steeringIntensity);
        turnAngle += steerInput * physicsConfig.turnRate * steeringMultiplier * dt;
        turnAngle *= (float)Math.Pow(physicsConfig.steeringDecay, dt);

        // Apply rotation to forward vector
        float rotationThisFrame = turnAngle * dt;

        // rotate forward around z axis
        float cos = (float)Math.Cos(rotationThisFrame);
        float sin = (float)Math.Sin(rotationThisFrame);
        float newX = forward.X * cos - forward.Y * sin;
        float newY = forward.X * sin + forward.Y * cos;
        forward = new Vector2(newX, newY);

        // Move forward
        position += forward * speed * dt;
    }

    public static void SimulateOneStepWithInputs(ref Vector2 position, ref Vector2 forward, ref float speed,
                                               ref float turnAngle, float dt, float simThrottleInput, float simSteerInput,
                                               MotorcyclePhysicsConfig physicsConfig)
    {
        SimulateOneStep(ref position, ref forward, ref speed, ref turnAngle, dt, simThrottleInput, simSteerInput, physicsConfig);
    }
    
    public static bool CheckIfPathGoesOffTrack(float steeringInput, Vector2 currentPosition, Vector2 currentForward, 
                                             float currentSpeed, float currentTurnAngle, float throttleInput, 
                                             float trajectoryLength, int recommendationSteps, float offTrackThreshold, 
                                             MotorcyclePhysicsConfig physicsConfig, out float offTrackRatio, PolygonTrack track)
    {
        // Simulation state - start with current state
        Vector2 simPosition = currentPosition;
        Vector2 simForward = currentForward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / recommendationSteps;
        int offTrackPoints = 0;
        int totalCheckPoints = Math.Min(recommendationSteps, 8); // Check first 8 points for performance
        
        // Simulate forward and check if any predicted positions are off track
        for (int step = 0; step < totalCheckPoints; step++)
        {
            // Simulate one step forward with current throttle and provided steering
            SimulateOneStepWithInputs(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, 
                                                        deltaTime, throttleInput, steeringInput, physicsConfig);
            
            // Check if this position is on track using raycast
            if (!track.PointInTrack(simPosition))
            {
                offTrackPoints++;
            }
        }
        
        // Consider path as "going off track" if more than the threshold of check points are off track
        offTrackRatio = (float)offTrackPoints / totalCheckPoints;
        bool goingOffTrack = offTrackRatio > offTrackThreshold;
        
        return goingOffTrack;
    }
}