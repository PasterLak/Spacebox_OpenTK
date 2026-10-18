namespace Spacebox.Game.Resource;

public class FallDamageJSON
{
    public float SafeHeight { get; set; } = 5f;
    public float DamagePerBlock { get; set; } = 4f;
}

public class GravityPresetJSON
{
    public string Name { get; set; } = "Earth";
    public float Strength { get; set; } = 9.81f;
    public FallDamageJSON? FallDamage { get; set; }
}

public class GravityGeneratorJSON
{
    public int Radius { get; set; } = 32;
    public int MinRadius { get; set; } = 4;
    public int MaxRadius { get; set; } = 512;
    public float Strength { get; set; } = 9.81f;
    public float MinStrength { get; set; } = 0.5f;
    public float MaxStrength { get; set; } = 30f;
    public int Power { get; set; } = 200;
    public float StrengthPowerFactor { get; set; } = 0.25f;
    public int MaxRoomCells { get; set; } = 200000;
    public FallDamageJSON FallDamage { get; set; } = new FallDamageJSON();
    public List<GravityPresetJSON> Presets { get; set; } = new List<GravityPresetJSON>
    {
        new GravityPresetJSON { Name = "Moon", Strength = 1.62f },
        new GravityPresetJSON { Name = "Mars", Strength = 3.71f },
        new GravityPresetJSON { Name = "Earth", Strength = 9.81f },
        new GravityPresetJSON { Name = "Jupiter", Strength = 24.79f }
    };
}

public class GravityGeneratorBlockData : BlockData
{
    private const int MaxConsumption = 8000;

    public int Radius { get; }
    public int MinRadius { get; }
    public int MaxRadius { get; }
    public float Strength { get; }
    public float MinStrength { get; }
    public float MaxStrength { get; }
    public ushort Power { get; }
    public float StrengthPowerFactor { get; }
    public int MaxRoomCells { get; }
    public IReadOnlyList<GravityPresetJSON> Presets { get; }
    public FallDamageJSON FallDamage { get; }

    public GravityGeneratorBlockData(BlockData baseBlock, GravityGeneratorJSON settings) : base(baseBlock)
    {
        settings ??= new GravityGeneratorJSON();
        MinRadius = Math.Clamp(settings.MinRadius, 1, 1024);
        MaxRadius = Math.Clamp(settings.MaxRadius, MinRadius, 1024);
        Radius = Math.Clamp(settings.Radius, MinRadius, MaxRadius);
        MinStrength = Math.Clamp(settings.MinStrength, 0.1f, 100f);
        MaxStrength = Math.Clamp(settings.MaxStrength, MinStrength, 100f);
        Strength = Math.Clamp(settings.Strength, MinStrength, MaxStrength);
        Power = (ushort)Math.Clamp(settings.Power, 0, MaxConsumption);
        StrengthPowerFactor = Math.Clamp(settings.StrengthPowerFactor, 0f, 1f);
        MaxRoomCells = Math.Max(1, settings.MaxRoomCells);
        Presets = settings.Presets ?? new List<GravityPresetJSON>();
        FallDamage = settings.FallDamage ?? new FallDamageJSON();
    }

    public FallDamageJSON GetFallDamage(float strength)
    {
        GravityPresetJSON? nearest = null;
        float nearestDifference = float.MaxValue;

        for (int i = 0; i < Presets.Count; i++)
        {
            float difference = MathF.Abs(Presets[i].Strength - strength);
            if (difference >= nearestDifference) continue;

            nearestDifference = difference;
            nearest = Presets[i];
        }

        return nearest?.FallDamage ?? FallDamage;
    }

    public ushort CalculatePower(int radius, float strength)
    {
        float radiusFactor = radius / (float)Radius;
        float strengthFactor = 1f + StrengthPowerFactor * (strength / Strength - 1f);
        return (ushort)Math.Clamp((int)MathF.Round(Power * radiusFactor * strengthFactor), 1, MaxConsumption);
    }
}
