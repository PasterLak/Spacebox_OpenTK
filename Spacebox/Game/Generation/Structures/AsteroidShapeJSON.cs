namespace Spacebox.Game.Generation.Structures;

public class AsteroidShapeJSON
{
    public float Stretch { get; set; } = 0.3f;
    public float Warp { get; set; } = 0.25f;
    public float Roughness { get; set; } = 0.3f;
    public AsteroidLobesJSON Lobes { get; set; } = new AsteroidLobesJSON();
    public AsteroidDoubleJSON Double { get; set; } = new AsteroidDoubleJSON();
    public AsteroidTorusJSON Torus { get; set; } = new AsteroidTorusJSON();
    public AsteroidBiteJSON Bite { get; set; } = new AsteroidBiteJSON();
    public AsteroidCratersJSON Craters { get; set; } = new AsteroidCratersJSON();
    public bool RemoveFloatingBlocks { get; set; } = true;
}

public class AsteroidLobesJSON
{
    public byte Chance { get; set; } = 50;
    public byte MinCount { get; set; } = 1;
    public byte MaxCount { get; set; } = 3;
    public float MinSize { get; set; } = 0.3f;
    public float MaxSize { get; set; } = 0.6f;
}

public class AsteroidDoubleJSON
{
    public byte Chance { get; set; } = 0;
    public float MinSize { get; set; } = 0.5f;
    public float MaxSize { get; set; } = 0.9f;
    public float Neck { get; set; } = 0.5f;
}

public class AsteroidTorusJSON
{
    public byte Chance { get; set; } = 0;
    public float MinTube { get; set; } = 0.25f;
    public float MaxTube { get; set; } = 0.4f;
}

public class AsteroidBiteJSON
{
    public byte Chance { get; set; } = 0;
    public float MinSize { get; set; } = 0.3f;
    public float MaxSize { get; set; } = 0.6f;
}

public class AsteroidCratersJSON
{
    public byte MinCount { get; set; } = 0;
    public byte MaxCount { get; set; } = 3;
    public float MinSize { get; set; } = 0.1f;
    public float MaxSize { get; set; } = 0.25f;
}
