# UI Integration Guide

## Option 1: Quick Integration (Minimal UI)

Add these simple UI elements to your existing Canvas:

### Basic Controls
```
Canvas (your existing)
└── ThreadedTrainingPanel
    ├── StartButton (Button - calls TrainingEngineInterface.StartTraining())
    ├── StopButton (Button - calls TrainingEngineInterface.StopTraining())
    ├── SpeedSlider (Slider - calls TrainingEngineInterface.SetTrainingSpeed())
    └── StatusText (Text - shows training status)
```

## Option 2: Full Integration (Complete UI)

Create a comprehensive training panel:

### Full Training Panel Layout
```
Canvas
└── ThreadedTrainingPanel
    ├── ControlsGroup
    │   ├── StartButton
    │   ├── StopButton
    │   ├── ResetButton
    │   └── SpeedControls
    │       ├── SpeedSlider
    │       └── SpeedText
    ├── ProgressGroup
    │   ├── SessionText
    │   ├── IterationText
    │   ├── ActiveAgentsText
    │   ├── BestTimeText
    │   └── ProgressBar
    ├── MetricsGroup
    │   ├── StepsPerSecText
    │   ├── TotalStepsText
    │   ├── ElapsedTimeText
    │   └── StatusText
    ├── ConfigGroup (only active when not training)
    │   ├── ThreadCountSlider
    │   ├── AgentsPerBatchSlider
    │   ├── IterationsSlider
    │   └── ExplorationSlider
    └── VisualizationGroup
        ├── ShowTrailsToggle
        ├── ShowPerformanceToggle
        └── MaxAgentsSlider
```

## Quick Setup Script

Add this to any GameObject to quickly create basic UI:

```csharp
[ContextMenu("Create Basic Training UI")]
public void CreateBasicUI()
{
    var canvas = FindObjectOfType<Canvas>();
    if (canvas == null) return;
    
    // Create panel
    var panel = new GameObject("ThreadedTrainingPanel");
    panel.transform.SetParent(canvas.transform);
    
    // Add UI components...
    // (Implementation details in the full script)
}
```

## Integration with Existing TrainingUIController

If you want to use both systems:

1. **Keep your existing TrainingUIController** for the original system
2. **Add ThreadedTrainingUIController** for the new system
3. **Use a toggle or menu** to switch between training modes
4. **Disable one when the other is active**

Example integration code:
```csharp
public class TrainingModeSelector : MonoBehaviour
{
    [SerializeField] private TrainingUIController originalTraining;
    [SerializeField] private ThreadedTrainingUIController threadedTraining;
    [SerializeField] private Toggle useThreadedTrainingToggle;
    
    void Start()
    {
        useThreadedTrainingToggle.onValueChanged.AddListener(OnTrainingModeChanged);
    }
    
    void OnTrainingModeChanged(bool useThreaded)
    {
        originalTraining.gameObject.SetActive(!useThreaded);
        threadedTraining.gameObject.SetActive(useThreaded);
    }
}
```