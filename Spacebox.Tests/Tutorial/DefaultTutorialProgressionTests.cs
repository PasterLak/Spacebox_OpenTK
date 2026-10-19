using System.Text.Json;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class DefaultTutorialProgressionTests
{
    private static string DefaultPath(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "GameSets", "Default", relative);
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(relative);
    }

    private static JsonElement Load(string relative) => JsonDocument.Parse(File.ReadAllText(DefaultPath(relative))).RootElement;

    private static IEnumerable<JsonElement> Entries(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray()) yield return item;
            yield break;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in property.Value.EnumerateArray()) yield return item;
        }
    }

    private static string Str(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : "";
    }

    private static int Int(JsonElement element, string name, int fallback = 0)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : fallback;
    }

    private sealed record TaskRef(string Tutorial, string Step, string Type, string Item, string Machine);

    private static IEnumerable<(string Tutorial, JsonElement Root)> Tutorials()
    {
        foreach (var file in Directory.GetFiles(DefaultPath("Tutorials"), "*.json"))
            yield return (Path.GetFileName(file), JsonDocument.Parse(File.ReadAllText(file)).RootElement);
    }

    private static IEnumerable<TaskRef> TasksInOrder(string tutorial, JsonElement root)
    {
        foreach (var chapter in root.GetProperty("Chapters").EnumerateArray())
        {
            if (!chapter.TryGetProperty("Steps", out var steps)) continue;
            foreach (var step in steps.EnumerateArray())
            {
                if (!step.TryGetProperty("Tasks", out var tasks)) continue;
                foreach (var task in Flatten(tasks))
                {
                    string item = Str(task, "Item");
                    if (item.Length == 0) item = Str(task, "Block");
                    yield return new TaskRef(tutorial, Str(step, "Id"), Str(task, "Type").ToLowerInvariant(), item, Str(task, "Machine"));
                }
            }
        }
    }

    private static IEnumerable<JsonElement> Flatten(JsonElement tasks)
    {
        foreach (var task in tasks.EnumerateArray())
        {
            yield return task;
            if (task.TryGetProperty("Tasks", out var children))
                foreach (var child in Flatten(children)) yield return child;
        }
    }

    private static IEnumerable<string> Rewards(JsonElement root)
    {
        foreach (var chapter in root.GetProperty("Chapters").EnumerateArray())
        {
            if (chapter.TryGetProperty("Reward", out var chapterRewards))
                foreach (var reward in chapterRewards.EnumerateArray()) yield return Str(reward, "Item");

            if (!chapter.TryGetProperty("Steps", out var steps)) continue;
            foreach (var step in steps.EnumerateArray())
            {
                if (!step.TryGetProperty("Reward", out var stepRewards)) continue;
                foreach (var reward in stepRewards.EnumerateArray()) yield return Str(reward, "Item");
            }
        }
    }

    [Fact]
    public void Rewards_AreNeverItemsTheTutorialAsksFor()
    {
        foreach (var (name, root) in Tutorials())
        {
            var used = new HashSet<string>(TasksInOrder(name, root).Where(t => t.Item.Length > 0).Select(t => t.Item), StringComparer.OrdinalIgnoreCase);
            foreach (var reward in Rewards(root))
                Assert.False(used.Contains(reward), $"{name}: reward '{reward}' is also needed by a tutorial task");
        }
    }

    [Fact]
    public void CraftAndProcessTasks_HaveRecipes()
    {
        var crafted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var blueprint in Load("crafting.json").GetProperty("Blueprints").EnumerateArray())
            crafted.Add(Str(blueprint.GetProperty("Product"), "Item"));

        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var catalog in Load("recipes.json").GetProperty("RecipeCatalog").EnumerateArray())
        {
            string machine = Str(catalog, "Type");
            foreach (var recipe in catalog.GetProperty("Recipes").EnumerateArray())
            {
                string product = Str(recipe.GetProperty("Product"), "Item");
                processed.Add(product);
                processed.Add(machine + ":" + product);
                processed.Add(machine + ":");
            }
        }

        foreach (var (name, root) in Tutorials())
        {
            foreach (var task in TasksInOrder(name, root))
            {
                if (task.Type == "craft" && task.Item.Length > 0)
                    Assert.True(crafted.Contains(task.Item), $"{name}/{task.Step}: nothing crafts '{task.Item}'");

                if (task.Type == "process")
                {
                    string key = task.Machine.Length > 0 ? task.Machine + ":" + task.Item : task.Item;
                    Assert.True(processed.Contains(key), $"{name}/{task.Step}: no {task.Machine} recipe makes '{task.Item}'");
                }
            }
        }
    }

    [Fact]
    public void BlockTasks_UseExistingBlocks()
    {
        var blocks = new HashSet<string>(Entries(Load("blocks.json")).Select(b => Str(b, "ID")), StringComparer.OrdinalIgnoreCase);

        foreach (var (name, root) in Tutorials())
            foreach (var task in TasksInOrder(name, root))
                if ((task.Type == "place_block" || task.Type == "use_block") && task.Item.Length > 0)
                    Assert.True(blocks.Contains(task.Item), $"{name}/{task.Step}: unknown block '{task.Item}'");
    }

    [Fact]
    public void Ores_AreOnlyRequestedAfterAStrongEnoughDrill()
    {
        var drillTiers = Entries(Load("items.json"))
            .Where(i => Str(i, "Type") == "drill")
            .ToDictionary(i => Str(i, "ID"), i => Int(i, "Tier"), StringComparer.OrdinalIgnoreCase);
        var hardness = Entries(Load("blocks.json"))
            .Where(b => Str(b, "Type") == "mineral")
            .ToDictionary(b => Str(b, "ID"), b => Int(b, "PowerToDrill", 1), StringComparer.OrdinalIgnoreCase);

        int startTier = 0;
        foreach (var start in Load("config.json").GetProperty("ItemsOnStart").EnumerateArray())
            if (drillTiers.TryGetValue(Str(start, "ID"), out int tier)) startTier = Math.Max(startTier, tier);

        foreach (var (name, root) in Tutorials())
        {
            int best = startTier;
            foreach (var task in TasksInOrder(name, root))
            {
                if (task.Type == "craft" && drillTiers.TryGetValue(task.Item, out int crafted)) best = Math.Max(best, crafted);
                if ((task.Type == "have_item" || task.Type == "destroy_block" || task.Type == "pickup") && hardness.TryGetValue(task.Item, out int needed) && !name.StartsWith("creative"))
                    Assert.True(needed <= best, $"{name}/{task.Step}: '{task.Item}' needs a tier {needed} drill, the best drill so far is tier {best}");
            }
        }
    }
}
