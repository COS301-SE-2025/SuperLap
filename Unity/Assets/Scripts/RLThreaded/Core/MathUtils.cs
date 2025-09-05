using System;

/// <summary>
/// Mathematical utilities for the training system
/// Pure C# implementation without Unity dependencies
/// </summary>
public static class MathUtils
{
    public const float Epsilon = 0.00001f;
    public const float PI = (float)Math.PI;
    public const float TwoPI = 2f * PI;
    public const float HalfPI = PI * 0.5f;
    public const float DegToRad = PI / 180f;
    public const float RadToDeg = 180f / PI;

    /// <summary>
    /// Clamps a value between min and max
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    public static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// Clamps a value between 0 and 1
    /// </summary>
    public static float Clamp01(float value)
    {
        return Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp01(t);
    }

    /// <summary>
    /// Normalize an angle to be between -PI and PI
    /// </summary>
    public static float NormalizeAngle(float angle)
    {
        while (angle > PI) angle -= TwoPI;
        while (angle < -PI) angle += TwoPI;
        return angle;
    }

    /// <summary>
    /// Convert angle in radians to a direction vector
    /// </summary>
    public static Vector2D AngleToDirection(float angleRad)
    {
        return new Vector2D((float)Math.Cos(angleRad), (float)Math.Sin(angleRad));
    }

    /// <summary>
    /// Convert direction vector to angle in radians
    /// </summary>
    public static float DirectionToAngle(Vector2D direction)
    {
        return (float)Math.Atan2(direction.y, direction.x);
    }

    /// <summary>
    /// Rotate a vector by the given angle in radians
    /// </summary>
    public static Vector2D RotateVector(Vector2D vector, float angleRad)
    {
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);
        return new Vector2D(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    /// <summary>
    /// Calculate the shortest angle between two angles in radians
    /// </summary>
    public static float AngleDifference(float angleA, float angleB)
    {
        float diff = NormalizeAngle(angleB - angleA);
        return diff;
    }

    /// <summary>
    /// Check if a point is approximately equal to another point
    /// </summary>
    public static bool Approximately(float a, float b, float threshold = Epsilon)
    {
        return Math.Abs(a - b) < threshold;
    }

    /// <summary>
    /// Check if a Vector2D is approximately equal to another
    /// </summary>
    public static bool Approximately(Vector2D a, Vector2D b, float threshold = Epsilon)
    {
        return Vector2D.SqrDistance(a, b) < threshold * threshold;
    }

    /// <summary>
    /// Get the sign of a number (-1, 0, or 1)
    /// </summary>
    public static int Sign(float value)
    {
        if (value > Epsilon) return 1;
        if (value < -Epsilon) return -1;
        return 0;
    }

    /// <summary>
    /// Smoothstep interpolation
    /// </summary>
    public static float SmoothStep(float t)
    {
        t = Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Calculate the closest point on a line segment to a given point
    /// </summary>
    public static Vector2D ClosestPointOnLineSegment(Vector2D point, Vector2D lineStart, Vector2D lineEnd)
    {
        Vector2D lineVector = lineEnd - lineStart;
        float lineLength = lineVector.Magnitude;
        
        if (lineLength < Epsilon)
            return lineStart;

        Vector2D lineDirection = lineVector / lineLength;
        Vector2D pointVector = point - lineStart;
        
        float projection = Vector2D.Dot(pointVector, lineDirection);
        projection = Clamp(projection, 0f, lineLength);
        
        return lineStart + lineDirection * projection;
    }

    /// <summary>
    /// Check if a point is inside a polygon using ray casting algorithm
    /// </summary>
    public static bool PointInPolygon(Vector2D point, Vector2D[] polygon)
    {
        int intersections = 0;
        int vertexCount = polygon.Length;

        for (int i = 0; i < vertexCount; i++)
        {
            Vector2D vertex1 = polygon[i];
            Vector2D vertex2 = polygon[(i + 1) % vertexCount];

            // Check if ray intersects with edge
            if (((vertex1.y <= point.y) && (point.y < vertex2.y)) ||
                ((vertex2.y <= point.y) && (point.y < vertex1.y)))
            {
                float intersectionX = vertex1.x + (point.y - vertex1.y) / (vertex2.y - vertex1.y) * (vertex2.x - vertex1.x);
                if (point.x < intersectionX)
                {
                    intersections++;
                }
            }
        }

        return (intersections % 2) == 1;
    }

    /// <summary>
    /// Calculate area of a triangle
    /// </summary>
    public static float TriangleArea(Vector2D a, Vector2D b, Vector2D c)
    {
        return Math.Abs((a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) * 0.5f);
    }
}