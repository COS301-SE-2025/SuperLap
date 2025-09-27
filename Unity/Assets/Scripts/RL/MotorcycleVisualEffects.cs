using UnityEngine;

public class MotorcycleVisualEffects : MonoBehaviour
{
    public void UpdateMotorcycleLeaning(Transform motorcycleModel, float currentSpeed, float currentTurnAngle, 
                                      float maxLeanAngle, float optimalLeanSpeed, float leanSpeed, 
                                      float minSteeringSpeed, float turnRate, float dt, ref float currentLeanAngle)
    {
        if (motorcycleModel == null) 
        {
            currentLeanAngle = 0f;
            return;
        }

        float targetLeanAngle = CalculateTargetLeanAngle(currentSpeed, currentTurnAngle, maxLeanAngle, 
                                                       optimalLeanSpeed, minSteeringSpeed, turnRate);
        
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSpeed * dt);
        
        motorcycleModel.localRotation = Quaternion.Euler(0, 0, -currentLeanAngle);
    }

    public void UpdateCameraFOV(Camera dynamicCamera, float currentSpeed, float currentAcceleration, 
                              float minFOV, float maxFOV, float maxFOVSpeed, float accelerationFOVBoost, 
                              float fovAdjustSpeed, float dt, ref float currentFOV)
    {
        if (dynamicCamera == null) 
        {
            currentFOV = 0f;
            return;
        }

        float targetFOV = CalculateTargetFOV(currentSpeed, currentAcceleration, minFOV, maxFOV, 
                                           maxFOVSpeed, accelerationFOVBoost);
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovAdjustSpeed * dt);
        
        dynamicCamera.fieldOfView = currentFOV;
    }

    private float CalculateTargetLeanAngle(float currentSpeed, float currentTurnAngle, float maxLeanAngle, 
                                         float optimalLeanSpeed, float minSteeringSpeed, float turnRate)
    {
        if (currentSpeed < minSteeringSpeed || Mathf.Abs(currentTurnAngle) < 0.1f)
        {
            return 0f;
        }

        float speedMultiplier = CalculateSpeedLeanMultiplier(currentSpeed, optimalLeanSpeed);
        
        float turnIntensity = Mathf.Abs(currentTurnAngle) / turnRate;
        turnIntensity = Mathf.Clamp01(turnIntensity);
        
        float baseLeanAngle = turnIntensity * maxLeanAngle * speedMultiplier;
        
        return Mathf.Sign(currentTurnAngle) * baseLeanAngle;
    }

    private float CalculateSpeedLeanMultiplier(float currentSpeed, float optimalLeanSpeed)
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

    private float CalculateTargetFOV(float currentSpeed, float currentAcceleration, float minFOV, float maxFOV, 
                                   float maxFOVSpeed, float accelerationFOVBoost)
    {
        float speedBasedFOV = Mathf.Lerp(minFOV, maxFOV, currentSpeed / maxFOVSpeed);
        
        float accelerationBoost = Mathf.Max(0f, currentAcceleration) * accelerationFOVBoost;
        
        float targetFOV = speedBasedFOV + accelerationBoost;
        
        return Mathf.Clamp(targetFOV, minFOV, maxFOV + (accelerationFOVBoost * 10f));
    }
}
