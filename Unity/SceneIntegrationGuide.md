# Manual Unity Scene Integration Steps

## Required GameObjects in Your Scene

### 1. **ThreadedTrainingEngine** (Main System)

**Create:**
1. Create Empty GameObject named "ThreadedTrainingEngine"
2. Add the `TrainingEngineInterface` component
3. Configure in Inspector:
   - **Thread Count**: 4 (or your CPU cores - 1)
   - **Agents Per Batch**: 25
   - **Iterations Per Session**: 500
   - **Exploration Rate**: 0.15
   - **Training Speed Multiplier**: 1.0
   - **Agent Visualization Prefab**: Drag your agent prefab here
   - **Agent Container**: Drag the AgentContainer GameObject here
   - **Max Visualized Agents**: 50

### 2. **AgentContainer** (Agent Parent)

**Create:**
1. Create Empty GameObject named "AgentContainer"
2. Position at (0, 0, 0)
3. This will be the parent for all training agent visualizations

### 3. **ThreadedTrainingUI** (Optional - for UI controls)

**Create:**
1. Create Empty GameObject named "ThreadedTrainingUI"
2. Add the `ThreadedTrainingUIController` component
3. Create UI elements (see UI Setup section below)

## Integration with Existing Systems

### **Integration with TrackMaster**

The system automatically integrates with your existing `TrackMaster`:
- Listens for `TrackMaster.OnTrackLoaded` event
- Uses `TrackMaster.GetCurrentRaceline()` for track data
- No changes needed to your existing track loading system

### **Keyboard Controls (Built-in)**

- **T**: Start/Stop training
- **+/=**: Increase training speed
- **-**: Decrease training speed

## Scene Hierarchy Example

```
YourScene
├── TrackMaster (your existing)
├── CheckpointManager (your existing)
├── ThreadedTrainingEngine ← NEW
│   └── TrainingEngineInterface
├── AgentContainer ← NEW
├── ThreadedTrainingUI ← NEW (optional)
│   └── ThreadedTrainingUIController
└── Canvas (your existing UI)
    └── ... your existing UI elements
```

## Testing the Integration

1. **Load your scene** with track data
2. **Press Play** in Unity
3. **Press T** to start training
4. **Check Console** for initialization messages
5. **Watch Scene** for agent visualizations appearing

## Troubleshooting

### Common Issues:

1. **"Training system not initialized"**
   - Ensure TrackMaster has loaded track data
   - Check that raceline data is available

2. **No agents visible**
   - Check that Agent Visualization Prefab is assigned
   - Verify Agent Container is assigned
   - Check Max Visualized Agents > 0

3. **Compilation errors**
   - Ensure all RLThreaded scripts are in the project
   - Check that Vector2D conversions are working

4. **Performance issues**
   - Reduce Thread Count or Agents Per Batch
   - Lower Max Visualized Agents
   - Increase Training Speed Multiplier for faster simulation