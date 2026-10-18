namespace Spacebox.Game.Mobs;

public sealed class MobDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Behaviour { get; init; } = "";
    public string ModelPath { get; init; } = "";
    public string TexturePath { get; init; } = "";
    public float Scale { get; init; } = 1f;
    public float Radius { get; init; } = 0.4f;
    public int Health { get; init; } = 20;
    public int MinDamage { get; init; } = 5;
    public int MaxDamage { get; init; } = 10;
    public float AttackRange { get; init; } = 1.6f;
    public float AttackCooldown { get; init; } = 1.2f;
    public float Speed { get; init; } = 2.5f;
    public float ChaseSpeed { get; init; } = 4f;
    public float FleeSpeed { get; init; } = 5f;
    public float TurnSpeed { get; init; } = 6f;
    public float SightRange { get; init; } = 14f;
    public float FieldOfView { get; init; } = 140f;
    public float HearingRange { get; init; } = 4f;
    public float ForgetTime { get; init; } = 4f;
    public float FleeChance { get; init; }
    public float EatTime { get; init; } = 3f;
    public float FoodSearchRadius { get; init; } = 8f;
    public IReadOnlySet<short>? EdibleBlocks { get; init; }
    public IReadOnlyList<Item> Drops { get; init; } = Array.Empty<Item>();
    public MobSpawnJSON Spawn { get; init; } = new MobSpawnJSON();

    public bool CanEat(short blockId)
    {
        if (blockId == 0) return false;
        return EdibleBlocks != null ? EdibleBlocks.Contains(blockId) : NaturalBlocks.Contains(blockId);
    }

    public int RollDamage(Random random)
    {
        int min = Math.Min(MinDamage, MaxDamage);
        int max = Math.Max(MinDamage, MaxDamage);
        return random.Next(min, max + 1);
    }

    public Item? RollDrop(Random random)
    {
        return Drops.Count == 0 ? null : Drops[random.Next(Drops.Count)];
    }
}
