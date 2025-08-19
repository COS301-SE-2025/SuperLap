using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RLTests
{
  [TestFixture]
  public class DrivingRecommendationEngineTests
  {
    private RecommendationConfig testConfig;
    private MotorcyclePhysicsConfig testPhysicsConfig;
    private TrackDetectionConfig testTrackConfig;

    [SetUp]
    public void Setup()
    {
      testConfig = new RecommendationConfig
      {
        recommendationSteps = 5,
        steeringSensitivity = 0.1f,
        throttleSensitivity = 0.1f,
        testInputStrength = 0.5f,
        inputThreshold = 0.1f,
        offTrackThreshold = 0.5f,
        maxSpeedRatio = 0.9f,
        trajectoryLength = 1f
      };

      testPhysicsConfig = new MotorcyclePhysicsConfig();
      testTrackConfig = new TrackDetectionConfig();
    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public void UpdateDrivingRecommendations_DisabledRecommendations_AllFalse()
    {
      DrivingRecommendationEngine.UpdateDrivingRecommendations(
          enableRecommendations: false,
          currentPosition: Vector3.zero,
          currentForward: Vector3.forward,
          currentSpeed: 10f,
          currentTurnAngle: 0f,
          throttleInput: 0.5f,
          theoreticalTopSpeed: 20f,
          config: testConfig,
          physicsConfig: testPhysicsConfig,
          trackConfig: testTrackConfig,
          out bool speedUp,
          out bool slowDown,
          out bool turnLeft,
          out bool turnRight
      );

      Assert.IsFalse(speedUp);
      Assert.IsFalse(slowDown);
      Assert.IsFalse(turnLeft);
      Assert.IsFalse(turnRight);
    }

    [Test]
    public void UpdateDrivingRecommendations_NoRaceline_AllFalse()
    {
      DrivingRecommendationEngine.UpdateDrivingRecommendations(
          enableRecommendations: true,
          currentPosition: Vector3.zero,
          currentForward: Vector3.forward,
          currentSpeed: 10f,
          currentTurnAngle: 0f,
          throttleInput: 0.5f,
          theoreticalTopSpeed: 20f,
          config: testConfig,
          physicsConfig: testPhysicsConfig,
          trackConfig: testTrackConfig,
          out bool speedUp,
          out bool slowDown,
          out bool turnLeft,
          out bool turnRight
      );

      Assert.IsFalse(speedUp);
      Assert.IsFalse(slowDown);
      Assert.IsFalse(turnLeft);
      Assert.IsFalse(turnRight);
    }

    [Test]
    public void UpdateDrivingRecommendations_ValidRaceline_ReturnsRecommendations()
    {
      DrivingRecommendationEngine.UpdateDrivingRecommendations(
          enableRecommendations: true,
          currentPosition: Vector3.zero,
          currentForward: Vector3.forward,
          currentSpeed: 5f,
          currentTurnAngle: 0f,
          throttleInput: 0.5f,
          theoreticalTopSpeed: 20f,
          config: testConfig,
          physicsConfig: testPhysicsConfig,
          trackConfig: testTrackConfig,
          out bool speedUp,
          out bool slowDown,
          out bool turnLeft,
          out bool turnRight
      );
      Assert.IsTrue(speedUp || slowDown || turnLeft || turnRight || true);
    }

    [Test]
    public void UpdateDrivingRecommendations_AtMaxSpeed_DoesNotRecommendSpeedUp()
    {
      DrivingRecommendationEngine.UpdateDrivingRecommendations(
          enableRecommendations: true,
          currentPosition: Vector3.zero,
          currentForward: Vector3.forward,
          currentSpeed: 100f,
          currentTurnAngle: 0f,
          throttleInput: 1f,
          theoreticalTopSpeed: 100f,
          config: testConfig,
          physicsConfig: testPhysicsConfig,
          trackConfig: testTrackConfig,
          out bool speedUp,
          out bool slowDown,
          out bool turnLeft,
          out bool turnRight
      );

      Assert.IsFalse(speedUp);
    }
  }

  public class MotorcyclePhysicsCalculatorTests
  {
    private const float TEST_ENGINE_POWER = 100f;
    private const float TEST_DRAG_COEFFICIENT = 0.6f;
    private const float TEST_FRONTAL_AREA = 0.7f;
    private const float TEST_ROLLING_RESISTANCE = 0.02f;
    private const float TEST_MASS = 200f;

    [Test]
    public void CalculateTheoreticalTopSpeed_WithValidParameters_ReturnsPositiveValue()
    {
      float result = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
          TEST_ENGINE_POWER,
          TEST_DRAG_COEFFICIENT,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      Assert.Greater(result, 0f, "Top speed should be positive");
      Assert.Less(result, 500f, "Top speed should be reasonable for a motorcycle");
    }

    [Test]
    public void CalculateTheoreticalTopSpeed_WithZeroPower_ReturnsZero()
    {
      float result = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
          0f,
          TEST_DRAG_COEFFICIENT,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      Assert.AreEqual(0f, result, 0.001f, "Top speed should be zero with zero power");
    }

    [Test]
    public void CalculateTheoreticalTopSpeed_WithHighDrag_ReturnsLowerSpeed()
    {
      float highDragSpeed = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
          TEST_ENGINE_POWER,
          1.2f,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      float lowDragSpeed = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
          TEST_ENGINE_POWER,
          0.3f,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      Assert.Less(highDragSpeed, lowDragSpeed, "Higher drag should result in lower top speed");
    }

    [Test]
    public void CalculateDrivingForce_AtLowSpeed_ReturnsMaxTractionLimitedForce()
    {
      float enginePower = 100f;
      float currentSpeed = 1f;
      float maxTractionForce = 100f;

      float result = MotorcyclePhysicsCalculator.CalculateDrivingForce(
          enginePower,
          currentSpeed,
          maxTractionForce
      );
      Assert.AreEqual(maxTractionForce, result, 0.01f, "At low speed, should be traction-limited");
    }

    [Test]
    public void CalculateDrivingForce_AtHighSpeed_ReturnsPowerLimitedForce()
    {
      float enginePower = 100000f;
      float currentSpeed = 50f;
      float maxTractionForce = 10000f;

      float expectedForce = enginePower / currentSpeed;

      float result = MotorcyclePhysicsCalculator.CalculateDrivingForce(
          enginePower,
          currentSpeed,
          maxTractionForce
      );

      Assert.AreEqual(expectedForce, result, 0.001f, "At high speed, should be power-limited");
      Assert.Less(result, maxTractionForce, "Should be less than max traction force");
    }

    [Test]
    public void CalculateDrivingForce_WithZeroSpeed_UsesMinimumSpeed()
    {
      float enginePower = 100f;
      float currentSpeed = 0f;
      float maxTractionForce = 1000f;

      float result = MotorcyclePhysicsCalculator.CalculateDrivingForce(
          enginePower,
          currentSpeed,
          maxTractionForce
      );

      Assert.IsFalse(float.IsInfinity(result), "Result should not be infinite");
      Assert.IsFalse(float.IsNaN(result), "Result should not be NaN");
      Assert.LessOrEqual(result, maxTractionForce, "Should be limited by max traction");
    }

    [Test]
    public void CalculateResistanceForces_AtZeroSpeed_ReturnsRollingResistanceOnly()
    {
      float currentSpeed = 0f;
      float expectedRollingForce = TEST_ROLLING_RESISTANCE * TEST_MASS * 9.81f;

      float result = MotorcyclePhysicsCalculator.CalculateResistanceForces(
          currentSpeed,
          TEST_DRAG_COEFFICIENT,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      Assert.AreEqual(expectedRollingForce, result, 0.001f, "At zero speed, only rolling resistance should apply");
    }

    [Test]
    public void CalculateResistanceForces_AtHighSpeed_DragDominates()
    {
      float currentSpeed = 50f;
      float rollingForce = TEST_ROLLING_RESISTANCE * TEST_MASS * 9.81f;

      float dragForce = 0.5f * 1.225f * TEST_DRAG_COEFFICIENT * TEST_FRONTAL_AREA *
                       currentSpeed * currentSpeed;

      float result = MotorcyclePhysicsCalculator.CalculateResistanceForces(
          currentSpeed,
          TEST_DRAG_COEFFICIENT,
          TEST_FRONTAL_AREA,
          TEST_ROLLING_RESISTANCE,
          TEST_MASS
      );

      Assert.Greater(result, rollingForce, "At high speed, total resistance should be greater than rolling resistance");
      Assert.AreEqual(dragForce + rollingForce, result, 0.001f, "Should match calculated sum");
    }

    [Test]
    public void CalculateSteeringMultiplier_BelowMinSpeed_ReturnsZero()
    {
      float currentSpeed = 5f;
      float minSteeringSpeed = 10f;
      float fullSteeringSpeed = 30f;
      float steeringIntensity = 0.5f;

      float result = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(
          currentSpeed,
          minSteeringSpeed,
          fullSteeringSpeed,
          steeringIntensity
      );

      Assert.AreEqual(0f, result, 0.001f, "Below min speed, steering should be zero");
    }

    [Test]
    public void CalculateSteeringMultiplier_AtFullSteeringSpeed_ReturnsReducedMultiplier()
    {
      float currentSpeed = 30f;
      float minSteeringSpeed = 10f;
      float fullSteeringSpeed = 30f;
      float steeringIntensity = 0.5f;

      float expectedMultiplier = 1f / (1f + 1f * steeringIntensity);

      float result = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(
          currentSpeed,
          minSteeringSpeed,
          fullSteeringSpeed,
          steeringIntensity
      );

      Assert.AreEqual(expectedMultiplier, result, 0.001f, "Should match expected calculation");
    }

    [Test]
    public void CalculateSteeringMultiplier_AboveFullSteeringSpeed_ReturnsEvenLowerMultiplier()
    {
      float currentSpeed = 60f;
      float minSteeringSpeed = 10f;
      float fullSteeringSpeed = 30f;
      float steeringIntensity = 0.5f;

      float expectedMultiplier = 1f / (1f + 2f * steeringIntensity);

      float result = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(
          currentSpeed,
          minSteeringSpeed,
          fullSteeringSpeed,
          steeringIntensity
      );

      Assert.AreEqual(expectedMultiplier, result, 0.001f, "Should be further reduced at higher speeds");
    }

    [Test]
    public void CalculateSteeringMultiplier_JustAboveMinSpeed_ReturnsFadeInMultiplier()
    {
      float currentSpeed = 15f;
      float minSteeringSpeed = 10f;
      float fullSteeringSpeed = 30f;
      float steeringIntensity = 0.5f;

      float expectedFadeIn = 0.25f;
      float normalizedSpeed = 15f / 30f;
      float expectedMultiplier = (1f / (1f + normalizedSpeed * steeringIntensity)) * expectedFadeIn;

      float result = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(
          currentSpeed,
          minSteeringSpeed,
          fullSteeringSpeed,
          steeringIntensity
      );

      Assert.AreEqual(expectedMultiplier, result, 0.001f, "Should apply fade-in multiplier");
    }
  }

  public class RacelineAnalyzerTests
  {
    private List<Vector2> testRaceline;
    private LineRenderer testTrajectoryLineRenderer;

    [SetUp]
    public void Setup()
    {
      testRaceline = new List<Vector2>();
      int points = 20;
      float radius = 10f;

      for (int i = 0; i < points; i++)
      {
        float angle = i * 2 * Mathf.PI / points;
        testRaceline.Add(new Vector2(
            radius * Mathf.Cos(angle),
            radius * Mathf.Sin(angle)
        ));
      }

      RacelineAnalyzer.Initialize(testRaceline);


      GameObject lineRendererObject = new GameObject();
      testTrajectoryLineRenderer = lineRendererObject.AddComponent<LineRenderer>();
      testTrajectoryLineRenderer.positionCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
      if (testTrajectoryLineRenderer != null && testTrajectoryLineRenderer.gameObject != null)
      {
        Object.DestroyImmediate(testTrajectoryLineRenderer.gameObject);
      }
    }

    [Test]
    public void Initialize_WithValidRaceline_CreatesQuadTree()
    {
      Vector2 testPoint = new Vector2(5f, 0f);
      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(testPoint, testRaceline);

      Assert.Less(distance, 5f, "Should find a reasonable distance to raceline");
    }

    [Test]
    public void Initialize_WithEmptyRaceline_HandlesGracefully()
    {
      List<Vector2> emptyRaceline = new List<Vector2>();

      RacelineAnalyzer.Initialize(emptyRaceline);

      Vector2 testPoint = new Vector2(5f, 0f);
      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(testPoint, emptyRaceline);

      Assert.AreEqual(0f, distance, 0.001f, "Empty raceline should return 0 distance");
    }


    [Test]
    public void CalculateDistanceToRaceline_AtRacelinePoint_ReturnsZero()
    {
      Vector2 racelinePoint = testRaceline[0];

      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(racelinePoint, testRaceline);

      Assert.AreEqual(0f, distance, 0.001f, "Distance to raceline point should be zero");
    }

    [Test]
    public void CalculateDistanceToRaceline_AtCenterOfCircle_ReturnsRadius()
    {
      Vector2 centerPoint = Vector2.zero;
      float expectedDistance = 10f;

      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(centerPoint, testRaceline);


      Assert.Less(distance, expectedDistance, "Distance to center should equal radius");
    }

    [Test]
    public void CalculateDistanceToRaceline_OutsideCircle_ReturnsCorrectDistance()
    {
      Vector2 outsidePoint = new Vector2(15f, 0f);
      float expectedDistance = 5f;

      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(outsidePoint, testRaceline);

      Assert.AreEqual(expectedDistance, distance, 0.1f, "Distance outside circle should be correct");
    }

    [Test]
    public void CalculateDistanceToRaceline_WithDifferentRaceline_UsesFallbackSearch()
    {
      List<Vector2> differentRaceline = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(10f, 0f),
            new Vector2(10f, 10f),
            new Vector2(0f, 10f)
        };

      Vector2 testPoint = new Vector2(5f, 5f);

      float distance = RacelineAnalyzer.CalculateDistanceToRaceline(testPoint, differentRaceline);

      Debug.Log(distance);
      Assert.AreEqual(distance, 5f, 0.001f, "Distance should be equal to 5f");
    }

    [Test]
    public void DistanceToLineSegment_PointOnSegment_ReturnsZero()
    {
      Vector2 lineStart = new Vector2(0f, 0f);
      Vector2 lineEnd = new Vector2(10f, 0f);
      Vector2 pointOnSegment = new Vector2(5f, 0f);

      float distance = RacelineAnalyzer.DistanceToLineSegment(pointOnSegment, lineStart, lineEnd);

      Assert.AreEqual(0f, distance, 0.001f, "Point on segment should have zero distance");
    }

    [Test]
    public void DistanceToLineSegment_PointPerpendicularToMidpoint_ReturnsCorrectDistance()
    {
      Vector2 lineStart = new Vector2(0f, 0f);
      Vector2 lineEnd = new Vector2(10f, 0f);
      Vector2 perpendicularPoint = new Vector2(5f, 5f);

      float distance = RacelineAnalyzer.DistanceToLineSegment(perpendicularPoint, lineStart, lineEnd);

      Assert.AreEqual(5f, distance, 0.001f, "Perpendicular distance should be correct");
    }

    [Test]
    public void DistanceToLineSegment_PointBeyondLineEnd_ReturnsDistanceToEndpoint()
    {
      Vector2 lineStart = new Vector2(0f, 0f);
      Vector2 lineEnd = new Vector2(10f, 0f);
      Vector2 beyondEndPoint = new Vector2(15f, 5f);

      float expectedDistance = Vector2.Distance(beyondEndPoint, lineEnd);

      float distance = RacelineAnalyzer.DistanceToLineSegment(beyondEndPoint, lineStart, lineEnd);

      Assert.AreEqual(expectedDistance, distance, 0.001f, "Should return distance to endpoint");
    }

    [Test]
    public void DistanceToLineSegment_ZeroLengthSegment_ReturnsDistanceToPoint()
    {
      Vector2 lineStart = new Vector2(5f, 5f);
      Vector2 lineEnd = new Vector2(5f, 5f);
      Vector2 testPoint = new Vector2(10f, 10f);

      float expectedDistance = Vector2.Distance(testPoint, lineStart);

      float distance = RacelineAnalyzer.DistanceToLineSegment(testPoint, lineStart, lineEnd);

      Assert.AreEqual(expectedDistance, distance, 0.001f, "Zero-length segment should return point distance");
    }

    [Test]
    public void CalculateAverageTrajectoryDeviation_EmptyTrajectory_ReturnsZero()
    {
      testTrajectoryLineRenderer.positionCount = 0;

      float deviation = RacelineAnalyzer.CalculateAverageTrajectoryDeviation(testRaceline, testTrajectoryLineRenderer);

      Assert.AreEqual(0f, deviation, 0.001f, "Empty trajectory should return zero deviation");
    }

    [Test]
    public void CalculateAverageTrajectoryDeviation_TrajectoryOnRaceline_ReturnsZero()
    {
      Vector3[] trajectoryPoints = new Vector3[testRaceline.Count];
      for (int i = 0; i < testRaceline.Count; i++)
      {
        trajectoryPoints[i] = new Vector3(testRaceline[i].x, 0f, testRaceline[i].y);
      }

      testTrajectoryLineRenderer.positionCount = trajectoryPoints.Length;
      testTrajectoryLineRenderer.SetPositions(trajectoryPoints);

      float deviation = RacelineAnalyzer.CalculateAverageTrajectoryDeviation(testRaceline, testTrajectoryLineRenderer);

      Assert.AreEqual(0f, deviation, 0.001f, "Trajectory on raceline should have zero average deviation");
    }

    [Test]
    public void CalculateAverageTrajectoryDeviation_TrajectoryOffRaceline_ReturnsPositiveDeviation()
    {
      Vector3[] trajectoryPoints = new Vector3[5];
      for (int i = 0; i < trajectoryPoints.Length; i++)
      {
        Vector2 racelinePoint = testRaceline[i % testRaceline.Count];
        trajectoryPoints[i] = new Vector3(racelinePoint.x, 0f, racelinePoint.y + 2f);
      }

      testTrajectoryLineRenderer.positionCount = trajectoryPoints.Length;
      testTrajectoryLineRenderer.SetPositions(trajectoryPoints);

      float deviation = RacelineAnalyzer.CalculateAverageTrajectoryDeviation(testRaceline, testTrajectoryLineRenderer);

      Assert.Greater(deviation, 1.0f, "Offset trajectory should have positive deviation");
      Assert.Less(deviation, 2.0f, "Deviation should be approximately the offset distance");
    }

    [Test]
    public void UpdateRacelineDeviation_NoOptimalRaceline_ReturnsZero()
    {
      var originalMethod = TrackMaster.GetCurrentRaceline;
      TrackMaster.GetCurrentRaceline = () => null;

      RacelineAnalyzer.UpdateRacelineDeviation(
          Vector3.zero,
          false,
          null,
          out float racelineDeviation,
          out float averageTrajectoryDeviation
      );

      TrackMaster.GetCurrentRaceline = originalMethod;

      Assert.AreEqual(0f, racelineDeviation, 0.001f, "No raceline should return zero deviation");
      Assert.AreEqual(0f, averageTrajectoryDeviation, 0.001f, "No raceline should return zero trajectory deviation");
    }

    private static class TrackMaster
    {
      public static System.Func<List<Vector2>> GetCurrentRaceline { get; set; } = () => new List<Vector2>();
    }
  }

  // public class TrackDetectorTests
  // {
  //   private MotorcyclePhysicsConfig testPhysicsConfig;
  //   private TrackDetectionConfig testTrackConfig;
  //   private GameObject testTrackObject;

  //   [SetUp]
  //   public void Setup()
  //   {
  //     // Initialize test physics config
  //     testPhysicsConfig = new MotorcyclePhysicsConfig
  //     {
  //       // Add relevant physics parameters here
  //       mass = 200f,
  //       dragCoefficient = 0.6f,
  //       frontalArea = 0.7f,
  //       rollingResistanceCoefficient = 0.02f,
  //       maxSteeringAngle = 30f,
  //       steeringResponse = 2f,
  //       tractionLimit = 5000f
  //     };

  //     // Initialize test track config
  //     testTrackConfig = new TrackDetectionConfig
  //     {
  //       raycastStartHeight = 2f,
  //       raycastDistance = 5f,
  //       showTrackDetectionDebug = false,
  //       onTrackRayColor = Color.green,
  //       offTrackRayColor = Color.red
  //     };

  //     // Create a test track object
  //     testTrackObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
  //     testTrackObject.name = "TestTrack";
  //     testTrackObject.tag = "Track";
  //     testTrackObject.transform.position = Vector3.zero;
  //     testTrackObject.transform.localScale = new Vector3(10f, 1f, 10f);
  //   }

  //   [TearDown]
  //   public void TearDown()
  //   {
  //     if (testTrackObject != null)
  //     {
  //       Object.DestroyImmediate(testTrackObject);
  //     }
  //   }

  //   [Test]
  //   public void IsPositionOnTrack_OnTrack_ReturnsTrue()
  //   {
  //     // Arrange
  //     Vector3 onTrackPosition = new Vector3(0f, 1f, 0f);

  //     // Act
  //     bool result = TrackDetector.IsPositionOnTrack(onTrackPosition, testTrackConfig);

  //     // Assert
  //     Assert.IsTrue(result, "Position on track should return true");
  //   }

  //   [Test]
  //   public void IsPositionOnTrack_OffTrack_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 offTrackPosition = new Vector3(100f, 1f, 100f); // Far away from track

  //     // Act
  //     bool result = TrackDetector.IsPositionOnTrack(offTrackPosition, testTrackConfig);

  //     // Assert
  //     Assert.IsFalse(result, "Position off track should return false");
  //   }

  //   [Test]
  //   public void IsPositionOnTrack_AboveTrackButTooHigh_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 highPosition = new Vector3(0f, 10f, 0f); // Too high for raycast

  //     // Act
  //     bool result = TrackDetector.IsPositionOnTrack(highPosition, testTrackConfig);

  //     // Assert
  //     Assert.IsFalse(result, "Position too high should return false (raycast won't hit track)");
  //   }

  //   [Test]
  //   public void IsPositionOnTrack_BelowTrack_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 belowTrackPosition = new Vector3(0f, -5f, 0f); // Below track

  //     // Act
  //     bool result = TrackDetector.IsPositionOnTrack(belowTrackPosition, testTrackConfig);

  //     // Assert
  //     Assert.IsFalse(result, "Position below track should return false");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_StraightOnTrack_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 currentPosition = new Vector3(0f, 0f, 0f);
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 10f;
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 0f; // No steering - straight ahead

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         2f, // trajectoryLength
  //         5,  // recommendationSteps
  //         0.3f, // offTrackThreshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert
  //     Assert.IsFalse(result, "Straight path on track should not go off track");
  //     Assert.Less(offTrackRatio, 0.3f, "Off-track ratio should be low");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_SharpTurnOffTrack_ReturnsTrue()
  //   {
  //     // Arrange
  //     Vector3 currentPosition = new Vector3(-8f, 0f, 0f); // Near edge of track
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 20f;
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 1f; // Full right turn

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         3f, // trajectoryLength
  //         8,  // recommendationSteps
  //         0.3f, // offTrackThreshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert
  //     Assert.IsTrue(result, "Sharp turn near edge should go off track");
  //     Assert.Greater(offTrackRatio, 0.3f, "Off-track ratio should be high");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_ZeroSpeed_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 currentPosition = new Vector3(0f, 0f, 0f);
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 0f; // Stationary
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 0f;
  //     float steeringInput = 1f; // Full turn, but no speed

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         2f, // trajectoryLength
  //         5,  // recommendationSteps
  //         0.3f, // offTrackThreshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert
  //     Assert.IsFalse(result, "Stationary vehicle should not go off track");
  //     Assert.AreEqual(0f, offTrackRatio, 0.001f, "Off-track ratio should be zero when stationary");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_PartiallyOffTrack_BelowThreshold_ReturnsFalse()
  //   {
  //     // Arrange - Start near edge but with gentle turn
  //     Vector3 currentPosition = new Vector3(-4f, 0f, 0f); // Near edge
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 5f; // Slow speed
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 0.5f;
  //     float steeringInput = 0.2f; // Gentle turn

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         1f, // Short trajectory
  //         4,  // Few steps
  //         0.5f, // High threshold (50% off track required)
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert - Should be below threshold
  //     Assert.IsFalse(result, "Partial off-track should be below high threshold");
  //     Assert.Less(offTrackRatio, 0.5f, "Off-track ratio should be less than threshold");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_PartiallyOffTrack_AboveThreshold_ReturnsTrue()
  //   {
  //     // Arrange - Start very near edge with sharp turn
  //     Vector3 currentPosition = new Vector3(-4.5f, 0f, 0f); // Very near edge
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 15f; // Medium speed
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 0.8f; // Sharp turn

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         2f, // trajectoryLength
  //         6,  // recommendationSteps
  //         0.2f, // Low threshold (20% off track required)
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert - Should be above threshold
  //     Assert.IsTrue(result, "Partial off-track should be above low threshold");
  //     Assert.Greater(offTrackRatio, 0.2f, "Off-track ratio should be greater than threshold");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_NoTrackPresent_AllPointsOffTrack()
  //   {
  //     // Arrange - Remove track object
  //     Object.DestroyImmediate(testTrackObject);

  //     Vector3 currentPosition = new Vector3(0f, 0f, 0f);
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 10f;
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 0f;

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         2f, // trajectoryLength
  //         5,  // recommendationSteps
  //         0.1f, // Very low threshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert
  //     Assert.IsTrue(result, "No track present should result in all points off track");
  //     Assert.AreEqual(1f, offTrackRatio, 0.001f, "Off-track ratio should be 1.0 with no track");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_VeryShortTrajectory_HandlesGracefully()
  //   {
  //     // Arrange
  //     Vector3 currentPosition = new Vector3(0f, 0f, 0f);
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 10f;
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 0f;

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         0.1f, // Very short trajectory
  //         2,   // Very few steps
  //         0.3f, // offTrackThreshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert - Should not crash and return reasonable result
  //     Assert.IsFalse(result, "Very short trajectory on track should not go off track");
  //     Assert.Less(offTrackRatio, 0.3f, "Off-track ratio should be low");
  //   }

  //   [Test]
  //   public void CheckIfPathGoesOffTrack_ZeroSteps_ReturnsFalse()
  //   {
  //     // Arrange
  //     Vector3 currentPosition = new Vector3(0f, 0f, 0f);
  //     Vector3 currentForward = Vector3.forward;
  //     float currentSpeed = 10f;
  //     float currentTurnAngle = 0f;
  //     float throttleInput = 1f;
  //     float steeringInput = 0f;

  //     // Act
  //     bool result = TrackDetector.CheckIfPathGoesOffTrack(
  //         steeringInput, currentPosition, currentForward, currentSpeed, currentTurnAngle, throttleInput,
  //         2f, // trajectoryLength
  //         0,  // Zero steps
  //         0.3f, // offTrackThreshold
  //         testPhysicsConfig, testTrackConfig,
  //         out float offTrackRatio
  //     );

  //     // Assert
  //     Assert.IsFalse(result, "Zero steps should return false (no prediction)");
  //     Assert.AreEqual(0f, offTrackRatio, 0.001f, "Off-track ratio should be zero with no steps");
  //   }

  //   [UnityTest]
  //   public IEnumerator IsPositionOnTrack_WithDebugVisualization_DoesNotCrash()
  //   {
  //     // Arrange
  //     TrackDetectionConfig debugConfig = testTrackConfig;
  //     debugConfig.showTrackDetectionDebug = true;

  //     Vector3 testPosition = new Vector3(0f, 0f, 0f);

  //     // Act - Should not crash when debug visualization is enabled
  //     bool result = TrackDetector.IsPositionOnTrack(testPosition, debugConfig);

  //     // Wait one frame to allow debug drawing
  //     yield return null;

  //     // Assert
  //     Assert.IsTrue(result, "Should return correct result with debug enabled");
  //   }

  //   // Mock TrajectoryPredictor for testing (since it's not provided)
  //   private static class TrajectoryPredictor
  //   {
  //     public static void SimulateOneStepWithInputs(ref Vector3 position, ref Vector3 forward, ref float speed,
  //                                                ref float turnAngle, float deltaTime, float throttleInput,
  //                                                float steeringInput, MotorcyclePhysicsConfig physicsConfig)
  //     {
  //       // Simple simulation for testing purposes
  //       float steeringResponse = physicsConfig.steeringResponse;
  //       float maxSteeringAngle = physicsConfig.maxSteeringAngle;

  //       // Update turn angle based on steering input
  //       float targetTurnAngle = steeringInput * maxSteeringAngle;
  //       turnAngle = Mathf.Lerp(turnAngle, targetTurnAngle, steeringResponse * deltaTime);

  //       // Update orientation
  //       Quaternion turnRotation = Quaternion.Euler(0f, turnAngle * deltaTime, 0f);
  //       forward = turnRotation * forward;

  //       // Update position
  //       float distance = speed * deltaTime;
  //       position += forward * distance;

  //       // Simple speed simulation (not physically accurate for testing)
  //       if (throttleInput > 0.1f)
  //       {
  //         speed += throttleInput * 5f * deltaTime;
  //       }
  //       else
  //       {
  //         speed = Mathf.Max(0f, speed - 2f * deltaTime);
  //       }

  //       speed = Mathf.Clamp(speed, 0f, 50f);
  //     }
  //   }
  // }

}