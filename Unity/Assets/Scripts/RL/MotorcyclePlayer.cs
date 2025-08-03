using System;
using UnityEngine;
using TMPro;
using Unity.MLAgents;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

public class MotorcyclePlayer : MonoBehaviour
{
    private const float AIR_DENSITY = 1.225f;
    private const float GRAVITY = 9.81f;

    [Header("Engine & Power")]
    [Tooltip("Engine power in Watts. ~750W = 1 horsepower.")]
    [SerializeField] private float enginePower = 150000f;
    [Tooltip("Maximum force the wheel can apply before slipping (in Newtons).")]
    [SerializeField] private float maxTractionForce = 7000f;
    [Tooltip("Force applied when braking (in Newtons).")]
    [SerializeField] private float brakingForce = 8000f;

    [Header("Physical Properties")]
    [Tooltip("Total mass of the bike and rider in KG.")]
    [SerializeField] private float mass = 200f;
    [Tooltip("Coefficient of drag. Lower is more aerodynamic.")]
    [SerializeField] private float dragCoefficient = 0.6f;
    [Tooltip("Frontal area of the bike/rider in square meters.")]
    [SerializeField] private float frontalArea = 0.48f;
    [Tooltip("Coefficient of rolling resistance.")]
    [SerializeField] private float rollingResistanceCoefficient = 0.012f;

    [Header("Turning")]
    [Tooltip("How fast the bike turns in degrees/sec.")]
    [SerializeField] private float turnRate = 100f;
    [Tooltip("How quickly the steering returns to center (0-1 range).")]
    [SerializeField] private float steeringDecay = 0.9f;
    [Tooltip("Minimum speed (m/s) required to steer.")]
    [SerializeField] private float minSteeringSpeed = 0.5f;
    [Tooltip("Speed (m/s) at which steering becomes fully responsive.")]
    [SerializeField] private float fullSteeringSpeed = 5f;
    [Tooltip("Controls how aggressively steering reduces with speed. Higher = more reduction. (0.1-2.0 recommended)")]
    [SerializeField] private float steeringIntensity = 0.5f;

    [Header("Motorcycle Banking/Leaning")]
    [Tooltip("Reference to the motorcycle model that will tilt. Should be a child transform.")]
    [SerializeField] private Transform motorcycleModel;
    [Tooltip("Maximum lean angle in degrees.")]
    [SerializeField] private float maxLeanAngle = 45f;
    [Tooltip("Speed (m/s) at which maximum lean is achieved.")]
    [SerializeField] private float optimalLeanSpeed = 15f;
    [Tooltip("How quickly the bike leans into turns (0-1 range).")]
    [SerializeField] private float leanSpeed = 5f;

    [Header("Dynamic Camera")]
    [Tooltip("Optional camera to adjust FOV based on speed and acceleration.")]
    [SerializeField] private Camera dynamicCamera;
    [Tooltip("Minimum field of view when stationary.")]
    [SerializeField] private float minFOV = 60f;
    [Tooltip("Maximum field of view at top speed.")]
    [SerializeField] private float maxFOV = 90f;
    [Tooltip("Speed (m/s) at which maximum FOV is reached.")]
    [SerializeField] private float maxFOVSpeed = 50f;
    [Tooltip("Additional FOV boost per m/s² of acceleration.")]
    [SerializeField] private float accelerationFOVBoost = 2f;
    [Tooltip("How quickly the FOV adjusts to changes.")]
    [SerializeField] private float fovAdjustSpeed = 3f;

    [Header("Trajectory Prediction")]
    [Tooltip("Enable/disable trajectory line visualization.")]
    [SerializeField] private bool showTrajectory = false;
    [Tooltip("Length of the predicted trajectory in seconds.")]
    [SerializeField] private float trajectoryLength = 3f;
    [Tooltip("Number of points to calculate for the trajectory line.")]
    [SerializeField] private int trajectoryPoints = 50;
    [Tooltip("Color of the trajectory line.")]
    [SerializeField] private Color trajectoryColor = Color.green;
    [Tooltip("Width of the trajectory line.")]
    [SerializeField] private float trajectoryWidth = 0.1f;

    [Header("GUI")]
    [Tooltip("Displays the current speed of the motorcycle.")]
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Current State (for debugging)")]
    [ReadOnly] [SerializeField] private float currentSpeed = 0f;
    [ReadOnly] [SerializeField] private float currentSpeedKmh = 0f;
    [ReadOnly] [SerializeField] private float currentAcceleration = 0f;
    [ReadOnly] [SerializeField] private float currentTurnAngle = 0f;
    [ReadOnly] [SerializeField] private float currentLeanAngle = 0f;
    [ReadOnly] [SerializeField] private float currentFOV = 0f;
    [ReadOnly] [SerializeField] private float theoreticalTopSpeed = 0f;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;
    
    // Internal tracking
    private float previousSpeed = 0f;
    
    // Trajectory prediction
    private LineRenderer trajectoryLineRenderer;

    void Start()
    {
        CalculateTheoreticalTopSpeed();
        SetupTrajectoryLineRenderer();
    }

    void Update()
    {
        HandleInput();
        UpdateMovement(Time.deltaTime);
        UpdateTurning(Time.deltaTime);
        UpdateMotorcycleLeaning(Time.deltaTime);
        UpdateCameraFOV(Time.deltaTime);
        UpdateTrajectoryVisualization();

        currentSpeedKmh = currentSpeed * 3.6f;
        if (speedText != null)
        {
            speedText.text = $"{currentSpeedKmh:F1} km/h";
        }
    }

    private void HandleInput()
    {
        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    private void UpdateMovement(float dt)
    {
        previousSpeed = currentSpeed;

        float drivingForce = CalculateDrivingForce();
        float resistanceForces = CalculateResistanceForces();

        float netForce = (drivingForce * throttleInput) - resistanceForces;

        if (throttleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;
        currentAcceleration = acceleration;

        currentSpeed += acceleration * dt;

        currentSpeed = Mathf.Max(0, currentSpeed);

        transform.Translate(Vector3.forward * currentSpeed * dt, Space.Self);
    }

    private void UpdateTurning(float dt)
    {
        float steeringMultiplier = CalculateSteeringMultiplier();
        
        currentTurnAngle += steerInput * turnRate * steeringMultiplier * dt;
        
        currentTurnAngle *= Mathf.Pow(steeringDecay, dt);

        transform.Rotate(Vector3.up * currentTurnAngle * dt);
    }

    private void UpdateMotorcycleLeaning(float dt)
    {
        if (motorcycleModel == null) return;

        float targetLeanAngle = CalculateTargetLeanAngle();
        
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSpeed * dt);
        
        motorcycleModel.localRotation = Quaternion.Euler(0, 0, -currentLeanAngle);
    }

    private void UpdateCameraFOV(float dt)
    {
        if (dynamicCamera == null) return;

        float targetFOV = CalculateTargetFOV();
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovAdjustSpeed * dt);
        
        dynamicCamera.fieldOfView = currentFOV;
    }

    private float CalculateDrivingForce()
    {
        float powerLimitedForce = enginePower / Mathf.Max(currentSpeed, 0.1f);
        
        return Mathf.Min(maxTractionForce, powerLimitedForce);
    }
    
    private float CalculateResistanceForces()
    {
        float dragForce = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * currentSpeed * currentSpeed;

        float rollingForce = rollingResistanceCoefficient * mass * GRAVITY;

        return dragForce + rollingForce;
    }

    private float CalculateSteeringMultiplier()
    {
        if (currentSpeed < minSteeringSpeed)
        {
            return 0f;
        }

        float normalizedSpeed = currentSpeed / fullSteeringSpeed;
        
        float steeringMultiplier = 1f / (1f + normalizedSpeed * steeringIntensity);
        
        float fadeInMultiplier = Mathf.Clamp01((currentSpeed - minSteeringSpeed) / (fullSteeringSpeed - minSteeringSpeed));
        
        return steeringMultiplier * fadeInMultiplier;
    }

    private float CalculateTargetLeanAngle()
    {
        if (currentSpeed < minSteeringSpeed || Mathf.Abs(currentTurnAngle) < 0.1f)
        {
            return 0f;
        }

        float speedMultiplier = CalculateSpeedLeanMultiplier();
        
        float turnIntensity = Mathf.Abs(currentTurnAngle) / turnRate;
        turnIntensity = Mathf.Clamp01(turnIntensity);
        
        float baseLeanAngle = turnIntensity * maxLeanAngle * speedMultiplier;
        
        return Mathf.Sign(currentTurnAngle) * baseLeanAngle;
    }

    private float CalculateSpeedLeanMultiplier()
    {
        if (currentSpeed <= optimalLeanSpeed)
        {
            return Mathf.Lerp(0.2f, 1f, currentSpeed / optimalLeanSpeed);
        }
        else
        {
            float excessSpeed = currentSpeed - optimalLeanSpeed;
            float reductionFactor = 1f / (1f + excessSpeed * 0.02f);
            return Mathf.Max(0.3f, reductionFactor);
        }
    }

    private float CalculateTargetFOV()
    {
        float speedBasedFOV = Mathf.Lerp(minFOV, maxFOV, currentSpeed / maxFOVSpeed);
        
        float accelerationBoost = Mathf.Max(0f, currentAcceleration) * accelerationFOVBoost;
        
        float targetFOV = speedBasedFOV + accelerationBoost;
        
        return Mathf.Clamp(targetFOV, minFOV, maxFOV + (accelerationFOVBoost * 10f));
    }

    private void CalculateTheoreticalTopSpeed()
    {
        float dragTerm = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea;
        float rollingTerm = rollingResistanceCoefficient * mass * GRAVITY;
        
        theoreticalTopSpeed = Mathf.Pow(enginePower / dragTerm, 1f/3f);
        
        Debug.Log($"Theoretical Top Speed: {theoreticalTopSpeed * 3.6f:F1} km/h");
        Debug.Log($"Cd x A = {dragCoefficient * frontalArea:F3}");
    }

    private void SetupTrajectoryLineRenderer()
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
    }

    private void UpdateTrajectoryVisualization()
    {
        if (trajectoryLineRenderer == null)
        {
            SetupTrajectoryLineRenderer();
            return;
        }

        trajectoryLineRenderer.enabled = showTrajectory;
        trajectoryLineRenderer.material.color = trajectoryColor;
        trajectoryLineRenderer.startWidth = trajectoryWidth;
        trajectoryLineRenderer.endWidth = trajectoryWidth;

        if (!showTrajectory) return;

        CalculateTrajectoryPoints();
    }

    private void CalculateTrajectoryPoints()
    {
        Vector3[] points = new Vector3[trajectoryPoints];
        
        // Simulation state - start with current state
        Vector3 simPosition = transform.position;
        Vector3 simForward = transform.forward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = simPosition;

            // Simulate one step forward using the same physics as the actual motorcycle
            SimulateOneStep(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, deltaTime);
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.SetPositions(points);
    }

    private void SimulateOneStep(ref Vector3 position, ref Vector3 forward, ref float speed, ref float turnAngle, float dt)
    {
        // Use current inputs for prediction (could be modified to use different inputs)
        float simThrottleInput = throttleInput;
        float simSteerInput = steerInput;

        // Simulate movement using same physics as UpdateMovement
        float drivingForce = SimulateDrivingForce(speed);
        float resistanceForces = SimulateResistanceForces(speed);

        float netForce = (drivingForce * simThrottleInput) - resistanceForces;

        if (simThrottleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;
        speed += acceleration * dt;
        speed = Mathf.Max(0, speed);

        // Simulate turning using same physics as UpdateTurning
        float steeringMultiplier = SimulateSteeringMultiplier(speed);
        turnAngle += simSteerInput * turnRate * steeringMultiplier * dt;
        turnAngle *= Mathf.Pow(steeringDecay, dt);

        // Apply rotation to forward vector
        float rotationThisFrame = turnAngle * dt;
        Quaternion rotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
        forward = rotation * forward;

        // Move forward
        position += forward * speed * dt;
    }

    private float SimulateDrivingForce(float speed)
    {
        float powerLimitedForce = enginePower / Mathf.Max(speed, 0.1f);
        return Mathf.Min(maxTractionForce, powerLimitedForce);
    }

    private float SimulateResistanceForces(float speed)
    {
        float dragForce = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * speed * speed;
        float rollingForce = rollingResistanceCoefficient * mass * GRAVITY;
        return dragForce + rollingForce;
    }

    private float SimulateSteeringMultiplier(float speed)
    {
        if (speed < minSteeringSpeed)
        {
            return 0f;
        }

        float normalizedSpeed = speed / fullSteeringSpeed;
        float steeringMultiplier = 1f / (1f + normalizedSpeed * steeringIntensity);
        float fadeInMultiplier = Mathf.Clamp01((speed - minSteeringSpeed) / (fullSteeringSpeed - minSteeringSpeed));
        
        return steeringMultiplier * fadeInMultiplier;
    }
}
