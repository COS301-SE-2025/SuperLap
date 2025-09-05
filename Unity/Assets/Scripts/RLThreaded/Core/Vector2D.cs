using System;

/// <summary>
/// Pure C# 2D vector implementation to replace Unity's Vector2
/// Thread-safe and Unity-independent
/// </summary>
[System.Serializable]
public struct Vector2D : IEquatable<Vector2D>
{
    public float x;
    public float y;

    public Vector2D(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    // Static properties
    public static Vector2D Zero => new Vector2D(0, 0);
    public static Vector2D One => new Vector2D(1, 1);
    public static Vector2D Up => new Vector2D(0, 1);
    public static Vector2D Down => new Vector2D(0, -1);
    public static Vector2D Left => new Vector2D(-1, 0);
    public static Vector2D Right => new Vector2D(1, 0);

    // Properties
    public float Magnitude => (float)Math.Sqrt(x * x + y * y);
    public float SqrMagnitude => x * x + y * y;
    public Vector2D Normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > 0.00001f ? new Vector2D(x / mag, y / mag) : Zero;
        }
    }

    // Operators
    public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.x + b.x, a.y + b.y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.x - b.x, a.y - b.y);
    public static Vector2D operator -(Vector2D a) => new Vector2D(-a.x, -a.y);
    public static Vector2D operator *(Vector2D a, float scalar) => new Vector2D(a.x * scalar, a.y * scalar);
    public static Vector2D operator *(float scalar, Vector2D a) => new Vector2D(a.x * scalar, a.y * scalar);
    public static Vector2D operator /(Vector2D a, float scalar) => new Vector2D(a.x / scalar, a.y / scalar);

    public static bool operator ==(Vector2D a, Vector2D b) => Math.Abs(a.x - b.x) < 0.00001f && Math.Abs(a.y - b.y) < 0.00001f;
    public static bool operator !=(Vector2D a, Vector2D b) => !(a == b);

    // Static methods
    public static float Distance(Vector2D a, Vector2D b) => (a - b).Magnitude;
    public static float SqrDistance(Vector2D a, Vector2D b) => (a - b).SqrMagnitude;
    public static float Dot(Vector2D a, Vector2D b) => a.x * b.x + a.y * b.y;
    public static Vector2D Lerp(Vector2D a, Vector2D b, float t)
    {
        t = Math.Max(0, Math.Min(1, t));
        return a + (b - a) * t;
    }

    public static Vector2D Perpendicular(Vector2D vector) => new Vector2D(-vector.y, vector.x);

    // Instance methods
    public void Normalize()
    {
        float mag = Magnitude;
        if (mag > 0.00001f)
        {
            x /= mag;
            y /= mag;
        }
        else
        {
            x = 0;
            y = 0;
        }
    }

    public override string ToString() => $"({x:F3}, {y:F3})";
    public override bool Equals(object obj) => obj is Vector2D other && Equals(other);
    public bool Equals(Vector2D other) => this == other;
    public override int GetHashCode() => HashCode.Combine(x, y);

    // Unity conversion methods (for interface layer)
#if UNITY_2017_1_OR_NEWER
    public static implicit operator UnityEngine.Vector2(Vector2D v) => new UnityEngine.Vector2(v.x, v.y);
    public static implicit operator Vector2D(UnityEngine.Vector2 v) => new Vector2D(v.x, v.y);
    public static implicit operator UnityEngine.Vector3(Vector2D v) => new UnityEngine.Vector3(v.x, 0, v.y);
    public static Vector2D FromUnityVector3(UnityEngine.Vector3 v) => new Vector2D(v.x, v.z);
#endif
}