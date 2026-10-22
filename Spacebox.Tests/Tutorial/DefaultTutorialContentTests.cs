using System.Text.Json;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class DefaultTutorialContentTests
{
    private sealed class DefaultIds : ITutorialItemResolver
    {
        private readonly Dictionary<string, short> _ids = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);

        public DefaultIds()
        {
            short next = 1;
            foreach (var file in new[] { "items.json", "blocks.json" })
                foreach (var id in ReadIds(DefaultGameSetContent.Flat(file)))
                    if (!_ids.ContainsKey(id)) _ids[id] = next++;
        }

        public bool TryResolve(string id, out short itemId)
        {
            itemId = 0;
            return !string.IsNullOrWhiteSpace(id) && _ids.TryGetValue(id, out itemId);
        }

        private static IEnumerable<string> ReadIds(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("ID", out var id) && id.ValueKind == JsonValueKind.String) yield return id.GetString();
                foreach (var property in element.EnumerateObject())
                    foreach (var child in ReadIds(property.Value)) yield return child;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    foreach (var child in ReadIds(item)) yield return child;
            }
        }
    }

    private static string GameSetFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "Mods", "GameSets", "Default", relative);
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(relative);
    }

    private static HashSet<string> InputActions()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(GameSetFile("config.json")));
        string path = Path.Combine(dir.Parent.Parent.Parent.FullName, "Resources", "default_input.json");
        var root = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var actions = new HashSet<string>(StringComparer.Ordinal);
        Collect(root, actions);
        return actions;

        static void Collect(JsonElement element, HashSet<string> actions)
        {
            if (element.ValueKind != JsonValueKind.Object) return;
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object && property.Value.TryGetProperty("Bindings", out _)) actions.Add(property.Name);
                else Collect(property.Value, actions);
            }
        }
    }

    private static List<TutorialDefinition> LoadAll(List<string> warnings)
    {
        var parser = new TutorialParser(new DefaultIds(), warnings);
        var result = new List<TutorialDefinition>();
        foreach (var file in Directory.GetFiles(GameSetFile("Tutorials"), "*.json"))
            result.Add(parser.Parse(File.ReadAllText(file), Path.GetFileName(file)));
        return result;
    }

    private static IEnumerable<TutorialTaskDefinition> AllTasks(TutorialDefinition tutorial)
    {
        foreach (var chapter in tutorial.Chapters)
            foreach (var step in chapter.Steps)
                foreach (var task in step.Tasks)
                {
                    yield return task;
                    foreach (var child in task.Children) yield return child;
                }
    }

    [Fact]
    public void DefaultTutorials_ParseWithoutWarnings()
    {
        var warnings = new List<string>();
        var tutorials = LoadAll(warnings);

        Assert.Empty(warnings);
        Assert.NotEmpty(tutorials);
        Assert.All(tutorials, Assert.NotNull);
    }

    [Fact]
    public void DefaultTutorials_CoverSurvivalAndCreative()
    {
        var tutorials = LoadAll(new List<string>());

        Assert.Contains(tutorials, t => t.SupportsMode(GameMode.Survival) && t.Chapters.Count >= 8);
        Assert.Contains(tutorials, t => t.SupportsMode(GameMode.Creative));
    }

    [Fact]
    public void DefaultTutorials_UseExistingInputActionsAndStats()
    {
        var actions = InputActions();

        foreach (var tutorial in LoadAll(new List<string>()))
        {
            foreach (var task in AllTasks(tutorial))
            {
                if (!string.IsNullOrWhiteSpace(task.Data.Action)) Assert.Contains(task.Data.Action, actions);
                if (task.Type == "stat") Assert.True(PlayerStatReader.IsKnown(task.Data.Stat), task.Data.Stat);
            }

            foreach (var chapter in tutorial.Chapters)
                foreach (var step in chapter.Steps)
                    foreach (var token in Tokens(step.Text + step.Hint + step.Title))
                        Assert.Contains(token, actions);
        }

        Assert.Contains("tutorial", actions);
    }

    private static IEnumerable<string> Tokens(string text)
    {
        int index = 0;
        while ((index = text.IndexOf("{key:", index, StringComparison.Ordinal)) >= 0)
        {
            int end = text.IndexOf('}', index);
            yield return text.Substring(index + 5, end - index - 5);
            index = end;
        }
    }
}
