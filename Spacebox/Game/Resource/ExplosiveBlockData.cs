namespace Spacebox.Game.Resource;

public class ExplosiveJSON
{
    public float Radius { get; set; } = 4f;
    public int Penetration { get; set; } = 100;
    public int Damage { get; set; } = 50;
    public bool DamageFalloff { get; set; } = true;
    public float PushStrength { get; set; } = 15f;
    public float PushRadiusMultiplier { get; set; } = 2f;
    public float ChainDelay { get; set; } = 0.15f;
    public bool DropBlocks { get; set; } = false;
    public bool DestroyStorageItems { get; set; } = false;
}

public class ExplosiveBlockData : BlockData
{
    public float Radius { get; }
    public int Penetration { get; }
    public int Damage { get; }
    public bool DamageFalloff { get; }
    public float PushStrength { get; }
    public float PushRadius { get; }
    public float ChainDelay { get; }
    public bool DropBlocks { get; }
    public bool DestroyStorageItems { get; }

    public ExplosiveBlockData(BlockData baseBlock, ExplosiveJSON settings) : base(baseBlock)
    {
        settings ??= new ExplosiveJSON();
        Radius = Math.Clamp(settings.Radius, 1f, 32f);
        Penetration = Math.Clamp(settings.Penetration, 0, 10000);
        Damage = Math.Clamp(settings.Damage, 0, 10000);
        DamageFalloff = settings.DamageFalloff;
        PushStrength = Math.Clamp(settings.PushStrength, 0f, 200f);
        PushRadius = Radius * Math.Clamp(settings.PushRadiusMultiplier, 1f, 8f);
        ChainDelay = Math.Clamp(settings.ChainDelay, 0f, 5f);
        DropBlocks = settings.DropBlocks;
        DestroyStorageItems = settings.DestroyStorageItems;
    }

    public float GetDamage(float distance)
    {
        if (distance > Radius) return 0f;
        return DamageFalloff ? Damage * (1f - distance / Radius) : Damage;
    }
}
