using Engine;
using Engine.Utils;
using Spacebox.Game.Mobs;

namespace Spacebox.Game.Resource;

public static class MobsLoader
{
    private const string FolderName = "Mobs";

    public static void Load(string modPath, string defaultModPath)
    {
        GameAssets.Mobs.Clear();

        string directory = Path.Combine(modPath, FolderName);
        if (!Directory.Exists(directory)) directory = Path.Combine(defaultModPath, FolderName);
        if (!Directory.Exists(directory)) return;

        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var json = JsonFixer.LoadJsonSafe<MobJSON>(file);
            if (json == null || string.IsNullOrWhiteSpace(json.Id))
            {
                Debug.Error($"[MobsLoader] Invalid mob file: {Path.GetFileName(file)}");
                continue;
            }

            if (!MobBrainFactory.IsRegistered(json.Behaviour))
            {
                Debug.Error($"[MobsLoader] Mob '{json.Id}' has unknown behaviour '{json.Behaviour}'");
                continue;
            }

            GameAssets.Mobs[json.Id] = Convert(json);
        }
    }

    private static MobDefinition Convert(MobJSON json)
    {
        return new MobDefinition
        {
            Id = json.Id,
            Name = string.IsNullOrWhiteSpace(json.Name) ? json.Id : json.Name,
            Behaviour = json.Behaviour,
            ModelPath = json.Model,
            TexturePath = json.Texture,
            Scale = MathF.Max(0.05f, json.Scale),
            Radius = Math.Clamp(json.Radius, 0.1f, 0.49f),
            Health = Math.Max(1, json.Health),
            MinDamage = Math.Max(0, json.MinDamage),
            MaxDamage = Math.Max(0, json.MaxDamage),
            AttackRange = MathF.Max(0.5f, json.AttackRange),
            AttackCooldown = MathF.Max(0.1f, json.AttackCooldown),
            Speed = MathF.Max(0f, json.Speed),
            ChaseSpeed = MathF.Max(0f, json.ChaseSpeed),
            FleeSpeed = MathF.Max(0f, json.FleeSpeed),
            TurnSpeed = MathF.Max(0.1f, json.TurnSpeed),
            SightRange = MathF.Max(0f, json.SightRange),
            FieldOfView = Math.Clamp(json.FieldOfView, 0f, 360f),
            HearingRange = MathF.Max(0f, json.HearingRange),
            ForgetTime = MathF.Max(0f, json.ForgetTime),
            FleeChance = Math.Clamp(json.FleeChance, (byte)0, (byte)100) / 100f,
            EatTime = MathF.Max(0.1f, json.EatTime),
            FoodSearchRadius = Math.Clamp(json.FoodSearchRadius, 1f, 32f),
            MealIntervalMin = MathF.Max(1f, MathF.Min(json.MealIntervalMin, json.MealIntervalMax)),
            MealIntervalMax = MathF.Max(1f, MathF.Max(json.MealIntervalMin, json.MealIntervalMax)),
            MealNearDistance = MathF.Max(0f, json.MealNearDistance),
            MealFarDistance = MathF.Max(json.MealNearDistance, json.MealFarDistance),
            EdibleBlocks = ResolveBlocks(json.Id, json.EdibleBlocks),
            LootTable = ResolveLoot(json.Id, json.Loot),
            Spawn = json.Spawn ?? new MobSpawnJSON(),
            Sounds = json.Sounds ?? new MobSoundsJSON()
        };
    }

    private static IReadOnlySet<short>? ResolveBlocks(string mobId, string[] ids)
    {
        if (ids == null || ids.Length == 0) return null;

        var result = new HashSet<short>();
        foreach (var id in ids)
        {
            if (GameAssets.BlocksStr.TryGetValue(ToFullId(id), out var block)) result.Add(block.Id);
            else Debug.Error($"[MobsLoader] Mob '{mobId}': unknown block '{id}'");
        }
        return result;
    }

    private static string ResolveLoot(string mobId, string loot)
    {
        if (string.IsNullOrWhiteSpace(loot)) return "";

        string table = Path.GetFileNameWithoutExtension(loot.Trim());
        if (GameAssets.LootConfig == null || !GameAssets.LootConfig.LootTables.ContainsKey(table))
            Debug.Error($"[MobsLoader] Mob '{mobId}': loot table '{table}' not found in Generation/Loot");
        return table;
    }

    private static string ToFullId(string id)
    {
        return id.Contains(':') ? id : GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, id);
    }
}
