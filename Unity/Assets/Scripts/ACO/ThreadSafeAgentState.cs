using System.Numerics;
using System.Collections.Generic;

[System.Serializable]
public class ThreadSafeAgentState
{
    private readonly object lockObject = new object();
    
    private Vector2 _position;
    private Vector2 _forward;
    private float _speed;
    private float _turnAngle;
    private bool _isActive;
    private bool _isOffTrack;
    private int _agentId;
    private List<(int, int)> _inputSequence;
    private float _timeToGoal;
    private bool _isValid;
    
    public Vector2 Position { get { lock(lockObject) { return _position; } } }
    public Vector2 Forward { get { lock(lockObject) { return _forward; } } }
    public float Speed { get { lock(lockObject) { return _speed; } } }
    public float TurnAngle { get { lock(lockObject) { return _turnAngle; } } }
    public bool IsActive { get { lock(lockObject) { return _isActive; } } }
    public bool IsOffTrack { get { lock(lockObject) { return _isOffTrack; } } }
    public int AgentId { get { lock(lockObject) { return _agentId; } } }
    public List<(int, int)> InputSequence { get { lock(lockObject) { return new List<(int, int)>(_inputSequence); } } }
    public float TimeToGoal { get { lock(lockObject) { return _timeToGoal; } } }
    public bool IsValid { get { lock(lockObject) { return _isValid; } } }
    
    public ThreadSafeAgentState(int agentId)
    {
        _agentId = agentId;
        _inputSequence = new List<(int, int)>();
        _isActive = false;
        _isOffTrack = false;
        _isValid = false;
    }
    
    public void UpdateState(Vector2 position, Vector2 forward, float speed, float turnAngle, bool isActive, bool isOffTrack)
    {
        lock(lockObject)
        {
            _position = position;
            _forward = forward;
            _speed = speed;
            _turnAngle = turnAngle;
            _isActive = isActive;
            _isOffTrack = isOffTrack;
        }
    }
    
    public void UpdateTrainingData(List<(int, int)> inputSequence, float timeToGoal, bool isValid)
    {
        lock(lockObject)
        {
            _inputSequence = new List<(int, int)>(inputSequence);
            _timeToGoal = timeToGoal;
            _isValid = isValid;
        }
    }
    
    public void SetInactive()
    {
        lock(lockObject)
        {
            _isActive = false;
        }
    }
}