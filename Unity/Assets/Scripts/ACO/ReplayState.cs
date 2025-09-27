using System;

public struct ReplayState
{
    public System.Numerics.Vector2 position;
    public float bear;
    public int throttle;

    public static ReplayState Parse(string input)
    {
        string[] parts = input.Split(':');
        float posX = float.Parse(parts[0]);
        float posY = float.Parse(parts[1]);
        float bearing = float.Parse(parts[2]);
        int throt = int.Parse(parts[3]);

        return new ReplayState
        {
            position = new System.Numerics.Vector2(posX, posY),
            bear = bearing,
            throttle = throt
        };
    }
    
    public override readonly string ToString()
    {
        return $"{position.X}:{position.Y}:{bear}:{throttle}";
    }
}