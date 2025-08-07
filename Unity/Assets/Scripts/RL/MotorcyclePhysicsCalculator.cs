using UnityEngine;

public static class MotorcyclePhysicsCalculator
{
    private const float AIR_DENSITY = 1.225f;
    private const float GRAVITY = 9.81f;

    public static float CalculateTheoreticalTopSpeed(float enginePower, float dragCoefficient, float frontalArea, 
                                                   float rollingResistanceCoefficient, float mass)
    {
        float dragTerm = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea;
        float rollingTerm = rollingResistanceCoefficient * mass * GRAVITY;
        
        float theoreticalTopSpeed = Mathf.Pow(enginePower / dragTerm, 1f/3f);
        
        Debug.Log($"Theoretical Top Speed: {theoreticalTopSpeed * 3.6f:F1} km/h");
        Debug.Log($"Cd x A = {dragCoefficient * frontalArea:F3}");
        
        return theoreticalTopSpeed;
    }

    public static float CalculateDrivingForce(float enginePower, float currentSpeed, float maxTractionForce)
    {
        float powerLimitedForce = enginePower / Mathf.Max(currentSpeed, 0.1f);
        return Mathf.Min(maxTractionForce, powerLimitedForce);
    }
    
    public static float CalculateResistanceForces(float currentSpeed, float dragCoefficient, float frontalArea, 
                                                float rollingResistanceCoefficient, float mass)
    {
        float dragForce = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * currentSpeed * currentSpeed;
        float rollingForce = rollingResistanceCoefficient * mass * GRAVITY;
        return dragForce + rollingForce;
    }

    public static float CalculateSteeringMultiplier(float currentSpeed, float minSteeringSpeed, 
                                                  float fullSteeringSpeed, float steeringIntensity)
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
}
