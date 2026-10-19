using System.Text;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Resource;

public class ThrusterBlockData : BlockData, IBlockTooltip, IReservedAreaBlockData
{
    public const int MaxNozzleLength = 16;

    public float Force { get; }
    public ushort Power { get; }
    public byte NozzleLength { get; }
    public bool AreaMustBeInsideEntity => false;

    public ThrusterBlockData(BlockData baseBlock, ThrusterJSON settings) : base(baseBlock)
    {
        settings ??= new ThrusterJSON();
        Force = Math.Clamp(settings.Force, 0f, 100000f);
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
        NozzleLength = (byte)Math.Clamp(settings.NozzleLength, 0, MaxNozzleLength);
    }

    public void GetReservedArea(Direction direction, Rotation rotation, List<Vector3i> offsets)
    {
        ThrusterNozzle.GetCells(this, NozzleLength, direction, rotation, offsets);
    }

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nThrust: {Force:0.#}");
        text.Append($"\nPower usage: {Power} EU");
        if (NozzleLength > 0) text.Append($"\nNozzle clearance: {NozzleLength} block{(NozzleLength == 1 ? "" : "s")}");
    }
}

public class ShipConsoleBlockData : BlockData, IBlockTooltip
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

    public void AppendTooltip(StringBuilder text)
    {
        text.Append(Weapons ? "\nControls: cannons" : Flight ? "\nControls: flight, thrusters, cannons" : "\nControls: thrusters, cannons");
        text.Append($"\nGroups: {Buttons}");
        text.Append($"\nPower usage: {Power} EU");
    }
}

public class CannonBlockData : BlockData, IBlockTooltip
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

    public void AppendTooltip(StringBuilder text)
    {
        if (GameAssets.TryGetProjectileByName(ProjectileName, out var projectile))
        {
            text.Append($"\nDamage: {projectile.Damage}/{projectile.DamageBlocks}");
            text.Append($"\nPenetration: {projectile.Penetration}");
            text.Append($"\nProjectile speed: {projectile.Speed:0.#} block/s");
            text.Append($"\nRange: {projectile.MaxTravelDistance} blocks");
        }

        text.Append($"\nReload time: {ReloadSeconds:0.0#}s");
        text.Append($"\nAim cone: {AimCone:0}°");
        text.Append($"\nTurn speed: {TurnSpeed:0}°/s");
        text.Append($"\nPower usage: {Power} EU");
    }
}

public class ShipCameraBlockData : BlockData, IBlockTooltip
{
    public ushort Power { get; }

    public ShipCameraBlockData(BlockData baseBlock, ShipCameraJSON settings) : base(baseBlock)
    {
        settings ??= new ShipCameraJSON();
        Power = (ushort)Math.Clamp(settings.Power, 0, 8000);
    }

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nPower usage: {Power} EU");
    }
}
