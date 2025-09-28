using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using RainbowArt.CleanFlatUI;

public class AISettingsPage : MonoBehaviour
{
  [Header("Pages")]
  [SerializeField] private GameObject settingsPage;
  [SerializeField] private GameObject previewPage;

  [Header("AI Preview Screen")]
  [SerializeField] private SelectorSimple Selector_Preview;
  [SerializeField] private SelectorSimple Selector_Settings;
  private int SectionCount = 10;

  [Header("Engine & Power")]
  [SerializeField] private Slider enginePowerSlider;
  [SerializeField] private Slider maxTractionForceSlider;
  [SerializeField] private Slider brakingForceSlider;
  [SerializeField] private float enginePower = 150000f;
  [SerializeField] private float maxTractionForce = 7000f;
  [SerializeField] private float brakingForce = 8000f;

  [Header("Physical Properties")]
  [SerializeField] private Slider massSlider;
  [SerializeField] private Slider dragCoefficientSlider;
  [SerializeField] private Slider frontalAreaSlider;
  [SerializeField] private Slider rollingResistanceCoefficientSlider;
  [SerializeField] private float mass = 200f;
  [SerializeField] private float dragCoefficient = 0.6f;
  [SerializeField] private float frontalArea = 0.48f;
  [SerializeField] private float rollingResistanceCoefficient = 0.012f;

  [Header("Turning")]
  [SerializeField] private Slider turnRateSlider;
  [SerializeField] private Slider minSteeringSpeedSlider;
  [SerializeField] private Slider fullSteeringSpeedSlider;
  [SerializeField] private float turnRate = 100f;
  [SerializeField] private float minSteeringSpeed = 0.5f;
  [SerializeField] private float fullSteeringSpeed = 5f;

  // Default values for reset
  private float defaultEnginePower, defaultMaxTractionForce, defaultBrakingForce;
  private float defaultMass, defaultDragCoefficient, defaultFrontalArea, defaultRollingResistanceCoefficient;
  private float defaultTurnRate, defaultMinSteeringSpeed, defaultFullSteeringSpeed;

  void Start()
  {
    StoreDefaults();
    SetupSelector();
    SetupSliderListeners();
    ViewSettings();
  }

  private void StoreDefaults()
  {
    defaultEnginePower = enginePower;
    defaultMaxTractionForce = maxTractionForce;
    defaultBrakingForce = brakingForce;

    defaultMass = mass;
    defaultDragCoefficient = dragCoefficient;
    defaultFrontalArea = frontalArea;
    defaultRollingResistanceCoefficient = rollingResistanceCoefficient;

    defaultTurnRate = turnRate;
    defaultMinSteeringSpeed = minSteeringSpeed;
    defaultFullSteeringSpeed = fullSteeringSpeed;
  }
  private bool isSyncingSelectors = false;
  private void SetupSelector()
  {
    if (Selector_Preview != null)
    {
      Selector_Preview.ClearOptions();
      List<string> options = new List<string>();
      for (int i = 1; i <= SectionCount; i++)
        options.Add($"Section {i}");
      Selector_Preview.AddOptions(options);

      Selector_Preview.OnValueChanged.AddListener(OnSelectorPreviewChanged);
    }

    if (Selector_Settings != null)
    {
      Selector_Settings.ClearOptions();
      List<string> options = new List<string>();
      for (int i = 1; i <= SectionCount; i++)
        options.Add($"Section {i}");
      Selector_Settings.AddOptions(options);

      Selector_Settings.OnValueChanged.AddListener(OnSelectorSettingsChanged);
    }
  }

  private void SetupSliderListeners()
  {
    if (enginePowerSlider != null) enginePowerSlider.onValueChanged.AddListener(ChangeEnginePower);
    if (maxTractionForceSlider != null) maxTractionForceSlider.onValueChanged.AddListener(ChangeMaxTractionForce);
    if (brakingForceSlider != null) brakingForceSlider.onValueChanged.AddListener(ChangeBrakingForce);

    if (massSlider != null) massSlider.onValueChanged.AddListener(ChangeMass);
    if (dragCoefficientSlider != null) dragCoefficientSlider.onValueChanged.AddListener(ChangeDragCoefficient);
    if (frontalAreaSlider != null) frontalAreaSlider.onValueChanged.AddListener(ChangeFrontalArea);
    if (rollingResistanceCoefficientSlider != null) rollingResistanceCoefficientSlider.onValueChanged.AddListener(ChangeRollingResistanceCoefficient);

    if (turnRateSlider != null) turnRateSlider.onValueChanged.AddListener(ChangeTurnRate);
    if (minSteeringSpeedSlider != null) minSteeringSpeedSlider.onValueChanged.AddListener(ChangeMinSteeringSpeed);
    if (fullSteeringSpeedSlider != null) fullSteeringSpeedSlider.onValueChanged.AddListener(ChangeFullSteeringSpeed);

    // Optionally sync sliders with current values
    SyncSlidersWithValues();
  }

  private void SyncSlidersWithValues()
  {
    if (enginePowerSlider != null) enginePowerSlider.value = enginePower;
    if (maxTractionForceSlider != null) maxTractionForceSlider.value = maxTractionForce;
    if (brakingForceSlider != null) brakingForceSlider.value = brakingForce;

    if (massSlider != null) massSlider.value = mass;
    if (dragCoefficientSlider != null) dragCoefficientSlider.value = dragCoefficient;
    if (frontalAreaSlider != null) frontalAreaSlider.value = frontalArea;
    if (rollingResistanceCoefficientSlider != null) rollingResistanceCoefficientSlider.value = rollingResistanceCoefficient;

    if (turnRateSlider != null) turnRateSlider.value = turnRate;
    if (minSteeringSpeedSlider != null) minSteeringSpeedSlider.value = minSteeringSpeed;
    if (fullSteeringSpeedSlider != null) fullSteeringSpeedSlider.value = fullSteeringSpeed;
  }

  // ---------------- CHANGE FUNCTIONS ----------------
  private void OnSelectorPreviewChanged(int index)
  {
    if (isSyncingSelectors) return; // prevent recursion
    isSyncingSelectors = true;
    Selector_Settings.CurrentIndex = index;
    Selector_Settings.StartIndex = index;
    isSyncingSelectors = false;
  }

  private void OnSelectorSettingsChanged(int index)
  {
    if (isSyncingSelectors) return; // prevent recursion
    isSyncingSelectors = true;
    Selector_Preview.CurrentIndex = index;
    Selector_Preview.StartIndex = index;
    isSyncingSelectors = false;
  }

  private IEnumerator WaitAndSync(SelectorSimple selector, int index)
  {
    yield return new WaitUntil(() => selector.gameObject.activeInHierarchy);
    Debug.Log("Became Active");
    selector.CurrentIndex = index;
    isSyncingSelectors = false;
  }

  private void ChangeEnginePower(float value) => enginePower = value;
  private void ChangeMaxTractionForce(float value) => maxTractionForce = value;
  private void ChangeBrakingForce(float value) => brakingForce = value;

  private void ChangeMass(float value) => mass = value;
  private void ChangeDragCoefficient(float value) => dragCoefficient = (float)System.Math.Round(value, 2);
  private void ChangeFrontalArea(float value) => frontalArea = (float)System.Math.Round(value, 2);
  private void ChangeRollingResistanceCoefficient(float value) => rollingResistanceCoefficient = (float)System.Math.Round(value, 3);

  private void ChangeTurnRate(float value) => turnRate = (float)System.Math.Round(value, 1);
  private void ChangeMinSteeringSpeed(float value) => minSteeringSpeed = value;
  private void ChangeFullSteeringSpeed(float value) => fullSteeringSpeed = value;

  // ---------------- RESET FUNCTION ----------------
  public void ResetSettings()
  {
    enginePower = defaultEnginePower;
    maxTractionForce = defaultMaxTractionForce;
    brakingForce = defaultBrakingForce;

    mass = defaultMass;
    dragCoefficient = defaultDragCoefficient;
    frontalArea = defaultFrontalArea;
    rollingResistanceCoefficient = defaultRollingResistanceCoefficient;

    turnRate = defaultTurnRate;
    minSteeringSpeed = defaultMinSteeringSpeed;
    fullSteeringSpeed = defaultFullSteeringSpeed;

    SyncSlidersWithValues();
    Debug.Log("All settings reset to default values.");
  }

  // ---------------- VIEW FUNCTIONS ----------------
  public void ViewSettings()
  {
    if (settingsPage != null) settingsPage.SetActive(true);
    if (previewPage != null) previewPage.SetActive(false);
  }

  public void ViewPreview()
  {
    if (settingsPage != null) settingsPage.SetActive(false);
    if (previewPage != null) previewPage.SetActive(true);

    // ACOTrainer trainer = new ACOTrainer();
    // trainer.StartTraining();
  }

  public void SaveSettings()
  {
    ACOAgent.SetParameters(enginePower , maxTractionForce , brakingForce , mass , dragCoefficient , frontalArea , rollingResistanceCoefficient , turnRate , minSteeringSpeed , fullSteeringSpeed);
  }
}
