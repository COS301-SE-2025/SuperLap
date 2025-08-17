using UnityEngine;

public class TrajectoryPredictor : MonoBehaviour
{
    private LineRenderer trajectoryLineRenderer;

    public void SetupTrajectoryLineRenderer(int trajectoryPoints, Color trajectoryColor, float trajectoryWidth, bool showTrajectory)
    {
        trajectoryLineRenderer = GetComponent<LineRenderer>();
        if (trajectoryLineRenderer == null)
        {
            trajectoryLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        trajectoryLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLineRenderer.material.color = trajectoryColor;
        trajectoryLineRenderer.startWidth = trajectoryWidth;
        trajectoryLineRenderer.endWidth = trajectoryWidth;
        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.useWorldSpace = true;
        trajectoryLineRenderer.enabled = showTrajectory;
        trajectoryLineRenderer.sortingOrder = 1;
    }

    public void UpdateTrajectoryVisualization(bool showTrajectory, Color trajectoryColor, float trajectoryWidth, 
                                            int trajectoryPoints, float trajectoryLength, Vector3 currentPosition, 
                                            Vector3 currentForward, float currentSpeed, float currentTurnAngle, 
                                            float throttleInput, float steerInput, MotorcyclePhysicsConfig physicsConfig)
    {
        if (trajectoryLineRenderer == null)
        {
            SetupTrajectoryLineRenderer(trajectoryPoints, trajectoryColor, trajectoryWidth, showTrajectory);
            return;
        }

        trajectoryLineRenderer.enabled = showTrajectory;
        trajectoryLineRenderer.material.color = trajectoryColor;
        trajectoryLineRenderer.startWidth = trajectoryWidth;
        trajectoryLineRenderer.endWidth = trajectoryWidth;

        if (!showTrajectory) return;

        CalculateTrajectoryPoints(trajectoryPoints, trajectoryLength, currentPosition, currentForward, 
                                currentSpeed, currentTurnAngle, throttleInput, steerInput, physicsConfig);
    }

    private void CalculateTrajectoryPoints(int trajectoryPoints, float trajectoryLength, Vector3 currentPosition, 
                                         Vector3 currentForward, float currentSpeed, float currentTurnAngle, 
                                         float throttleInput, float steerInput, MotorcyclePhysicsConfig physicsConfig)
    {
        Vector3[] points = new Vector3[trajectoryPoints];
        
        // Simulation state - start with current state
        Vector3 simPosition = currentPosition;
        Vector3 simForward = currentForward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = simPosition;
            SimulateOneStep(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, 
                          deltaTime, throttleInput, steerInput, physicsConfig);
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.SetPositions(points);
    }

    public static void SimulateOneStep(ref Vector3 position, ref Vector3 forward, ref float speed, ref float turnAngle, 
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
        speed = Mathf.Max(0, speed);

        // Simulate turning using same physics as UpdateTurning
        float steeringMultiplier = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(speed, physicsConfig.minSteeringSpeed, 
                                                                                         physicsConfig.fullSteeringSpeed, physicsConfig.steeringIntensity);
        turnAngle += steerInput * physicsConfig.turnRate * steeringMultiplier * dt;
        turnAngle *= Mathf.Pow(physicsConfig.steeringDecay, dt);

        // Apply rotation to forward vector
        float rotationThisFrame = turnAngle * dt;
        Quaternion rotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
        forward = rotation * forward;

        // Move forward
        position += forward * speed * dt;
    }

    public static void SimulateOneStepWithInputs(ref Vector3 position, ref Vector3 forward, ref float speed, 
                                               ref float turnAngle, float dt, float simThrottleInput, float simSteerInput, 
                                               MotorcyclePhysicsConfig physicsConfig)
    {
        SimulateOneStep(ref position, ref forward, ref speed, ref turnAngle, dt, simThrottleInput, simSteerInput, physicsConfig);
    }
}

[System.Serializable]
public struct MotorcyclePhysicsConfig
{
    public float enginePower;
    public float maxTractionForce;
    public float brakingForce;
    public float mass;
    public float dragCoefficient;
    public float frontalArea;
    public float rollingResistanceCoefficient;
    public float turnRate;
    public float steeringDecay;
    public float minSteeringSpeed;
    public float fullSteeringSpeed;
    public float steeringIntensity;
}
