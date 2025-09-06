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
}