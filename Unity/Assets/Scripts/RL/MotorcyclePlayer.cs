using System;
using UnityEngine;

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

    [Header("Current State (for debugging)")]
    [ReadOnly] [SerializeField] private float currentSpeed = 0f;
    [ReadOnly] [SerializeField] private float currentSpeedKmh = 0f;
    [ReadOnly] [SerializeField] private float currentTurnAngle = 0f;
    [ReadOnly] [SerializeField] private float theoreticalTopSpeed = 0f;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;

    void Start()
    {
        CalculateTheoreticalTopSpeed();
    }

    void Update()
    {
        HandleInput();
        UpdateMovement(Time.deltaTime);
        UpdateTurning(Time.deltaTime);
        
        currentSpeedKmh = currentSpeed * 3.6f;
    }

    private void HandleInput()
    {
        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    private void UpdateMovement(float dt)
    {
        float drivingForce = CalculateDrivingForce();
        float resistanceForces = CalculateResistanceForces();

        float netForce = (drivingForce * throttleInput) - resistanceForces;
        
        if (throttleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;

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

    private void CalculateTheoreticalTopSpeed()
    {
        float dragTerm = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea;
        float rollingTerm = rollingResistanceCoefficient * mass * GRAVITY;
        
        theoreticalTopSpeed = Mathf.Pow(enginePower / dragTerm, 1f/3f);
        
        Debug.Log($"Theoretical Top Speed: {theoreticalTopSpeed * 3.6f:F1} km/h");
        Debug.Log($"Cd x A = {dragCoefficient * frontalArea:F3}");
    }
}