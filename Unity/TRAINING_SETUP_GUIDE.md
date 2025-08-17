# RLMatrix Motorcycle Training Setup Guide (Unity Only)

## RLMatrix Overview
RLMatrix is a Unity-native reinforcement learning framework that requires NO Python code or external dependencies. Everything runs directly in Unity using C# attributes.

## Quick Setup Steps

### 1. Scene Setup
1. Use your existing scene with TrackMaster (or create a new one)
2. Ensure TrackMaster and your track are properly configured
3. Create an empty GameObject called "TrainingEnvironment"

**TrackMaster Integration**: The training system automatically integrates with your TrackMaster setup. Agents will:
- Wait for the track to load completely
- Spawn along the raceline at calculated positions
- Face the correct track direction
- Use the track data for observations and rewards

### 2. Create Agent Prefab
1. Create a new GameObject called "MotorcycleAgent"
2. Add the `MotorcycleRLMatrixAgent` component
3. Configure all the physics settings (copy from MotorcyclePlayer):
   - Engine Power: 150000W
   - Mass: 200kg  
   - Drag Coefficient: 0.6
   - Max Lean Angle: 45°

4. **RLMatrix Training Settings**:
   - Ray Count: 8 (for obstacle detection)
   - Ray Angle: 120° (wide sensing arc)
   - Max Ray Length: 15m
   - Max Episode Time: 120s

5. **Reward Settings**:
   - On Track Reward: 0.1
   - Off Track Penalty: -1.0
   - Speed Reward Scale: 0.02
   - Lean Angle Penalty: 0.01

6. Save as a prefab

### 3. Training Environment Setup (Optional)
If you want multiple agents training simultaneously:
1. Add the `MotorcycleTrainingEnvironment` script to your "TrainingEnvironment" GameObject
2. Assign your MotorcycleAgent prefab
3. Configure spawn points or let it auto-generate them

### 4. Start Training

#### Method 1: Single Agent Training
1. Simply place your MotorcycleRLMatrixAgent in the scene
2. Press Play in Unity
3. RLMatrix will automatically start training the agent
4. Use the Inspector to monitor progress and adjust settings

#### Method 2: Multi-Agent Training
1. Use the MotorcycleTrainingEnvironment setup
2. Press Play in Unity
3. Multiple agents will spawn and train simultaneously

### 5. Monitoring Training

**In Unity Inspector:**
- Watch the agent's reward values change
- Monitor episode statistics in the Training Environment
- Observe speed, lean angles, and raceline deviation

**Visual Indicators:**
- Green rays show the agent's environmental sensing
- Red ray shows track detection
- UI shows speed, rewards, and recommendations

### 6. Training Controls

**Observation Space:** The agent observes:
- Normalized speed and acceleration
- Turn angle and lean angle
- Relative position and orientation
- Environmental ray distances
- Track detection status
- Raceline deviation
- Driving recommendations

**Action Space:** The agent controls:
- Throttle/Brake (-1 to +1)
- Steering (-1 to +1)

**Reward System:**
- Positive reward for staying on track
- Speed-based rewards encourage good performance
- Penalties for excessive lean angles or erratic driving
- Raceline deviation penalties guide optimal racing

### 7. Heuristic Mode (Manual Control)
To test the agent manually:
1. Enable heuristic mode in RLMatrix settings
2. Use WASD or arrow keys to control the motorcycle
3. This helps verify the physics and reward system work correctly

## Key RLMatrix Attributes Used

The agent uses these RLMatrix attributes:
- `[RLMatrixEnvironment]` - Marks the class as a training environment
- `[RLMatrixObservation]` - Methods that return observation values
- `[RLMatrixActionContinuous(2)]` - Defines 2 continuous actions (throttle, steering)
- `[RLMatrixReward]` - Method that calculates rewards
- `[RLMatrixDone]` - Method that determines when episodes end
- `[RLMatrixReset]` - Method called when episodes reset
- `[RLMatrixHeuristic]` - Method for manual control during testing

## Training Process

### Expected Behavior Timeline
- **0-1k episodes**: Random behavior, learning basic controls
- **1k-10k episodes**: Following track, basic racing behavior
- **10k-50k episodes**: Optimizing speed, smoother racing
- **50k+ episodes**: Near-optimal performance, consistent lap times

### Success Indicators
- Consistent lap completion (90%+ success rate)
- Increasing average reward over time
- Smoother acceleration and braking patterns
- Appropriate lean angles in corners
- Minimal off-track incidents

### Troubleshooting

**Agent gets stuck or doesn't learn:**
- Increase off-track penalty
- Check that observations are normalized properly
- Verify reward signals are clear

**Agent too cautious:**
- Reduce penalties
- Increase speed rewards
- Check lean angle limits

**Agent too aggressive:**
- Increase lean angle penalty
- Add smoothness rewards
- Reduce speed reward scaling

## Advantages of RLMatrix

✅ **No Python Required** - Everything runs in Unity
✅ **Easy Setup** - Just add attributes to your C# code
✅ **Real-time Training** - Watch your agent learn in the editor
✅ **Unity Integration** - Full access to Unity's features during training
✅ **Simple Debugging** - Use Unity's debugging tools
✅ **No External Dependencies** - Self-contained solution

That's it! Your motorcycle agent is ready to start learning. Just press Play and watch it go from random movements to skilled racing! 🏍️
