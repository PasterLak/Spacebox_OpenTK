namespace Spacebox.Game.Mobs;

public class MobJSON
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Behaviour { get; set; } = "";
    public string Model { get; set; } = "";
    public string Texture { get; set; } = "";
    public float Scale { get; set; } = 1f;
    public float Radius { get; set; } = 0.4f;
    public int Health { get; set; } = 20;
    public int MinDamage { get; set; } = 5;
    public int MaxDamage { get; set; } = 10;
    public float AttackRange { get; set; } = 1.6f;
    public float AttackCooldown { get; set; } = 1.2f;
    public float Speed { get; set; } = 2.5f;
    public float ChaseSpeed { get; set; } = 4f;
    public float FleeSpeed { get; set; } = 5f;
    public float TurnSpeed { get; set; } = 6f;
    public float SightRange { get; set; } = 14f;
    public float FieldOfView { get; set; } = 140f;
    public float HearingRange { get; set; } = 4f;
    public float ForgetTime { get; set; } = 4f;
    public byte FleeChance { get; set; } = 0;
    public float EatTime { get; set; } = 3f;
    public float FoodSearchRadius { get; set; } = 8f;
    public string[] EdibleBlocks { get; set; } = Array.Empty<string>();
    public string[] Drops { get; set; } = Array.Empty<string>();
    public MobSpawnJSON Spawn { get; set; } = new MobSpawnJSON();
}

public class MobSpawnJSON
{
    public bool Enabled { get; set; } = false;
    public int MaxPerChunk { get; set; } = 1;
    public int MaxTotal { get; set; } = 16;
    public float MaxLightLevel { get; set; } = 0f;
    public bool RequiresNaturalBlocks { get; set; } = true;
    public float Interval { get; set; } = 5f;
    public byte Chance { get; set; } = 30;
    public float MinPlayerDistance { get; set; } = 16f;
    public float MaxPlayerDistance { get; set; } = 48f;
    public float DespawnDistance { get; set; } = 128f;
}
