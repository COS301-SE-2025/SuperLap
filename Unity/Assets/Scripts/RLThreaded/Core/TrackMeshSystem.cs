using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// High-performance track detection system using geometric calculations
/// Replaces Unity's Physics.Raycast with pure mathematical operations
/// Thread-safe and optimized for concurrent access
/// </summary>
public class TrackMeshSystem
{
    private readonly List<Vector2D> innerBoundary;
    private readonly List<Vector2D> outerBoundary;
    private readonly List<Vector2D> raceline;
    private readonly TrackMeshGrid spatialGrid;
    private readonly float[] trackWidths; // Precomputed track width at each raceline point
    
    // Track bounds for quick rejection tests
    private readonly Vector2D minBounds;
    private readonly Vector2D maxBounds;
    
    public TrackMeshSystem(List<Vector2D> innerBoundary, List<Vector2D> outerBoundary, List<Vector2D> raceline, float gridResolution = 1.0f)
    {
        this.innerBoundary = innerBoundary ?? throw new ArgumentNullException(nameof(innerBoundary));
        this.outerBoundary = outerBoundary ?? throw new ArgumentNullException(nameof(outerBoundary));
        this.raceline = raceline ?? throw new ArgumentNullException(nameof(raceline));
        
        // Calculate track bounds
        var allPoints = innerBoundary.Concat(outerBoundary);
        minBounds = new Vector2D(
            allPoints.Min(p => p.x),
            allPoints.Min(p => p.y)
        );
        maxBounds = new Vector2D(
            allPoints.Max(p => p.x),
            allPoints.Max(p => p.y)
        );
        
        // Precompute track widths
        trackWidths = PrecomputeTrackWidths();
        
        // Create spatial grid for fast lookups
        spatialGrid = new TrackMeshGrid(innerBoundary, outerBoundary, raceline, minBounds, maxBounds, gridResolution);
    }
    
    /// <summary>
    /// Check if a point is on the track using optimized geometric calculations
    /// </summary>
    public bool IsPointOnTrack(Vector2D position)
    {
        // Quick bounds check
        if (position.x < minBounds.x || position.x > maxBounds.x ||
            position.y < minBounds.y || position.y > maxBounds.y)
        {
            return false;
        }
        
        // Use spatial grid for fast lookup when possible
        if (spatialGrid.TryGetCachedResult(position, out bool cachedResult))
        {
            return cachedResult;
        }
        
        // Fall back to precise point-in-polygon test
        bool isInsideOuter = MathUtils.PointInPolygon(position, outerBoundary.ToArray());
        bool isInsideInner = MathUtils.PointInPolygon(position, innerBoundary.ToArray());
        
        bool isOnTrack = isInsideOuter && !isInsideInner;
        
        // Cache the result
        spatialGrid.CacheResult(position, isOnTrack);
        
        return isOnTrack;
    }
    
    /// <summary>
    /// Get the track width at a specific position
    /// </summary>
    public float GetTrackWidthAtPosition(Vector2D position)
    {
        if (!IsPointOnTrack(position))
        {
            return 0f;
        }
        
        // Find nearest raceline point and return precomputed width
        int nearestIndex = FindNearestRacelineIndex(position);
        return trackWidths[nearestIndex];
    }
    
    /// <summary>
    /// Get the nearest point on the raceline to a given position
    /// </summary>
    public Vector2D GetNearestRacelinePoint(Vector2D position)
    {
        int nearestIndex = FindNearestRacelineIndex(position);
        return raceline[nearestIndex];
    }
    
    /// <summary>
    /// Get the distance from a position to the raceline
    /// </summary>
    public float GetDistanceFromRaceline(Vector2D position)
    {
        Vector2D nearestPoint = GetNearestRacelinePoint(position);
        return Vector2D.Distance(position, nearestPoint);
    }
    
    /// <summary>
    /// Get the raceline direction at a specific position
    /// </summary>
    public Vector2D GetRacelineDirection(Vector2D position)
    {
        int nearestIndex = FindNearestRacelineIndex(position);
        
        // Calculate direction by looking ahead on the raceline
        int nextIndex = (nearestIndex + 1) % raceline.Count;
        Vector2D direction = (raceline[nextIndex] - raceline[nearestIndex]).Normalized;
        
        return direction;
    }
    
    /// <summary>
    /// Check if a trajectory goes off track
    /// Optimized version of the original TrackDetector functionality
    /// </summary>
    public bool DoesTrajectoryGoOffTrack(Vector2D startPosition, Vector2D startDirection, float startSpeed, 
                                       float startTurnAngle, AgentInput input, float trajectoryLength, 
                                       int steps, float offTrackThreshold, MotorcycleConfig config)
    {
        float deltaTime = trajectoryLength / steps;
        int offTrackPoints = 0;
        int totalCheckPoints = Math.Min(steps, 8); // Check first 8 points for performance
        
        Vector2D simPosition = startPosition;
        Vector2D simDirection = startDirection;
        float simSpeed = startSpeed;
        float simTurnAngle = startTurnAngle;
        
        for (int step = 0; step < totalCheckPoints; step++)
        {
            // Simulate one physics step
            PurePhysicsEngine.SimulateStep(ref simPosition, ref simDirection, ref simSpeed, ref simTurnAngle, 
                                         deltaTime, input, config);
            
            // Check if position is off track
            if (!IsPointOnTrack(simPosition))
            {
                offTrackPoints++;
            }
        }
        
        float offTrackRatio = (float)offTrackPoints / totalCheckPoints;
        return offTrackRatio > offTrackThreshold;
    }
    
    // Properties for external access
    public List<Vector2D> InnerBoundary => innerBoundary;
    public List<Vector2D> OuterBoundary => outerBoundary;
    public List<Vector2D> Raceline => raceline;
    public Vector2D MinBounds => minBounds;
    public Vector2D MaxBounds => maxBounds;
    
    #region Private Methods
    
    private float[] PrecomputeTrackWidths()
    {
        float[] widths = new float[raceline.Count];
        
        for (int i = 0; i < raceline.Count; i++)
        {
            Vector2D racelinePoint = raceline[i];
            
            // Find closest points on inner and outer boundaries
            float minInnerDist = float.MaxValue;
            float minOuterDist = float.MaxValue;
            
            foreach (var innerPoint in innerBoundary)
            {
                float dist = Vector2D.Distance(racelinePoint, innerPoint);
                if (dist < minInnerDist)
                {
                    minInnerDist = dist;
                }
            }
            
            foreach (var outerPoint in outerBoundary)
            {
                float dist = Vector2D.Distance(racelinePoint, outerPoint);
                if (dist < minOuterDist)
                {
                    minOuterDist = dist;
                }
            }
            
            widths[i] = minInnerDist + minOuterDist;
        }
        
        return widths;
    }
    
    private int FindNearestRacelineIndex(Vector2D position)
    {
        float minDistance = float.MaxValue;
        int nearestIndex = 0;
        
        for (int i = 0; i < raceline.Count; i++)
        {
            float distance = Vector2D.SqrDistance(position, raceline[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
    
    #endregion
}

/// <summary>
/// Spatial grid for fast track detection caching
/// Divides track space into grid cells for O(1) lookups
/// </summary>
public class TrackMeshGrid
{
    private struct GridCell
    {
        public bool hasCache;
        public bool isOnTrack;
        public float trackWidth;
        public int nearestRacelineIndex;
    }
    
    private readonly GridCell[,] grid;
    private readonly Vector2D gridOrigin;
    private readonly float cellSize;
    private readonly int gridWidth;
    private readonly int gridHeight;
    private readonly List<Vector2D> raceline;
    
    public TrackMeshGrid(List<Vector2D> innerBoundary, List<Vector2D> outerBoundary, List<Vector2D> raceline, 
                        Vector2D minBounds, Vector2D maxBounds, float cellSize)
    {
        this.cellSize = cellSize;
        this.raceline = raceline;
        this.gridOrigin = minBounds;
        
        // Calculate grid dimensions
        Vector2D size = maxBounds - minBounds;
        gridWidth = (int)Math.Ceiling(size.x / cellSize) + 1;
        gridHeight = (int)Math.Ceiling(size.y / cellSize) + 1;
        
        grid = new GridCell[gridWidth, gridHeight];
        
        // Initialize grid (no caching initially)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = new GridCell { hasCache = false };
            }
        }
    }
    
    public bool TryGetCachedResult(Vector2D position, out bool isOnTrack)
    {
        var gridCoords = WorldToGrid(position);
        if (IsValidGridCell(gridCoords))
        {
            var cell = grid[gridCoords.x, gridCoords.y];
            if (cell.hasCache)
            {
                isOnTrack = cell.isOnTrack;
                return true;
            }
        }
        
        isOnTrack = false;
        return false;
    }
    
    public void CacheResult(Vector2D position, bool isOnTrack)
    {
        var gridCoords = WorldToGrid(position);
        if (IsValidGridCell(gridCoords))
        {
            grid[gridCoords.x, gridCoords.y] = new GridCell
            {
                hasCache = true,
                isOnTrack = isOnTrack
            };
        }
    }
    
    private (int x, int y) WorldToGrid(Vector2D worldPosition)
    {
        Vector2D localPosition = worldPosition - gridOrigin;
        return ((int)(localPosition.x / cellSize), (int)(localPosition.y / cellSize));
    }
    
    private bool IsValidGridCell((int x, int y) gridCoords)
    {
        return gridCoords.x >= 0 && gridCoords.x < gridWidth &&
               gridCoords.y >= 0 && gridCoords.y < gridHeight;
    }
}