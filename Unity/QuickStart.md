# Quick Start - Minimal Integration

## Fastest Way to Test the System

### 1. **Create Agent Prefab** (2 minutes)
```
1. Create Empty GameObject → name "ThreadedAgent"
2. Add child Cube → scale to (0.5, 0.2, 1.0)
3. Add AgentVisualizationObject component to parent
4. Drag cube to "Agent Model" field
5. Drag cube's MeshRenderer to "Agent Renderer" field
6. Save as prefab in Assets/Prefabs/ThreadedAgentPrefab
```

### 2. **Add to Scene** (1 minute)
```
1. Create Empty GameObject → name "ThreadedTrainingEngine"
2. Add TrainingEngineInterface component
3. Create Empty GameObject → name "AgentContainer"
4. Drag ThreadedAgentPrefab to "Agent Visualization Prefab" field
5. Drag AgentContainer to "Agent Container" field
```

### 3. **Test** (30 seconds)
```
1. Press Play
2. Load a track (using your existing track loading)
3. Press T key to start training
4. Watch agents appear and move around the track
```

## Minimal Configuration

Use these settings for quick testing:
- **Thread Count**: 2
- **Agents Per Batch**: 10
- **Iterations Per Session**: 100
- **Max Visualized Agents**: 20

## Expected Results

You should see:
- ✅ Console message: "Training system initialized"
- ✅ Console message: "Training started with X threads"
- ✅ Small cubes moving around your track
- ✅ Performance metrics in console every 100 iterations

## If Something Goes Wrong

1. **Check Console** for error messages
2. **Verify track is loaded** before pressing T
3. **Check all references** are assigned in inspector
4. **Try reducing** Thread Count to 1 for debugging

## Next Steps

Once basic integration works:
1. **Replace cube with motorcycle model**
2. **Add UI controls** using ThreadedTrainingUIController
3. **Tune performance settings** for your hardware
4. **Compare performance** with original system