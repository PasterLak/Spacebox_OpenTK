namespace Spacebox.Game.Resource;

public class ThrusterBlockData : BlockData
{
    public float Force { get; }
    public ushort Power { get; }

    public ThrusterBlockData(BlockData baseBlock, ThrusterJSON settings) : base(baseBlock)
    {
        settings ??= new ThrusterJSON();
        Force = Math.Clamp(settings.Force, 0f, 100000f);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }
}

public class ShipConsoleBlockData : BlockData
{
    public const int MaxButtons = 16;

    public byte Buttons { get; }
    public ushort Power { get; }
    public bool Weapons { get; }
    public bool Flight { get; }

    public ShipConsoleBlockData(BlockData baseBlock, ShipConsoleJSON settings) : base(baseBlock)
    {
        settings ??= new ShipConsoleJSON();
        Buttons = (byte)Math.Clamp(settings.Buttons, 1, MaxButtons);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
        Weapons = settings.Weapons;
        Flight = settings.Flight && !settings.Weapons;
    }
}

public class CannonBlockData : BlockData
{
    public string ProjectileName { get; }
    public string ShotSound { get; }
    public float ReloadSeconds { get; }
    public float BarrelLength { get; }
    public float AimCone { get; }
    public float TurnSpeed { get; }
    public ushort Power { get; }

    public CannonBlockData(BlockData baseBlock, CannonJSON settings) : base(baseBlock)
    {
        settings ??= new CannonJSON();
        ProjectileName = (settings.Projectile ?? "").ToLowerInvariant();
        ShotSound = settings.ShotSound ?? "";
        ReloadSeconds = Math.Max(50, settings.ReloadTime) / 1000f;
        BarrelLength = Math.Max(0.5f, settings.BarrelLength);
        AimCone = Math.Clamp(settings.AimCone, 0f, 180f);
        TurnSpeed = Math.Clamp(settings.TurnSpeed, 1f, 3600f);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }
}

public class ShipCameraBlockData : BlockData
{
    public ushort Power { get; }

    public ShipCameraBlockData(BlockData baseBlock, ShipCameraJSON settings) : base(baseBlock)
    {
        settings ??= new ShipCameraJSON();
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }
}
