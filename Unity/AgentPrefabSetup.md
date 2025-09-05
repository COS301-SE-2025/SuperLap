# Agent Visualization Prefab Setup Instructions

## Create the Agent Visualization Prefab

1. **Create a new GameObject** in your scene:
   - Right-click in Hierarchy → Create Empty
   - Name it "ThreadedAgent"

2. **Add basic components**:
   - Add a **Cube** as child (for basic visualization)
   - Scale the cube to something small like (0.5, 0.2, 1.0)
   - Add a **Material** with a bright color (Green/Blue)

3. **Add the AgentVisualizationObject script**:
   - The script is already created in `TrainingVisualizationManager.cs`
   - Drag the script onto the ThreadedAgent GameObject

4. **Setup the components in the inspector**:
   - Agent Model: Drag the cube child object here
   - Agent Renderer: Drag the cube's MeshRenderer here
   - The script will auto-create performance UI elements

5. **Save as Prefab**:
   - Drag the GameObject to your Prefabs folder
   - Name it "ThreadedAgentPrefab"
   - Delete the GameObject from the scene

## Alternative: Use your existing MotorcycleAgent prefab
If you have an existing motorcycle model, you can:
- Use your motorcycle prefab
- Add the AgentVisualizationObject component to it
- Configure the references to point to your motorcycle model parts