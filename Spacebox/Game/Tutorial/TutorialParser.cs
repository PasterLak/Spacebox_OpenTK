using System.Text.Json;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial.Tasks;

namespace Spacebox.Game.Tutorial;

public interface ITutorialItemResolver
{
    bool TryResolve(string id, out short itemId);
}

public sealed class TutorialParser
{
    private const string UiIconPrefix = "ui:";

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly ITutorialItemResolver _items;
    private readonly List<string> _warnings;
    private string _source = "";

    public TutorialParser(ITutorialItemResolver items, List<string> warnings)
    {
        _items = items;
        _warnings = warnings;
    }

    public TutorialDefinition Parse(string json, string source)
    {
        _source = source ?? "";

        TutorialJSON data;
        try
        {
            data = JsonSerializer.Deserialize<TutorialJSON>(json, Options);
        }
        catch (JsonException ex)
        {
            Warn($"invalid JSON: {ex.Message}");
            return null;
        }

        if (data == null) return null;
        if (string.IsNullOrWhiteSpace(data.Id)) data.Id = Path.GetFileNameWithoutExtension(_source);

        var chapters = new List<TutorialChapter>();
        var stepIds = new HashSet<string>();
        var chaptersJson = data.Chapters ?? new List<TutorialChapterJSON>();
        for (int i = 0; i < chaptersJson.Count; i++)
        {
            var chapter = ParseChapter(chaptersJson[i], i, stepIds);
            if (chapter != null) chapters.Add(chapter);
        }

        if (chapters.Count == 0)
        {
            Warn("has no steps");
            return null;
        }

        return new TutorialDefinition(data, chapters, ParseModes(data.GameModes));
    }

    private TutorialChapter ParseChapter(TutorialChapterJSON data, int index, HashSet<string> stepIds)
    {
        if (data == null) return null;

        string id = string.IsNullOrWhiteSpace(data.Id) ? "chapter" + (index + 1) : data.Id;
        var steps = new List<TutorialStep>();
        var stepsJson = data.Steps ?? new List<TutorialStepJSON>();
        for (int i = 0; i < stepsJson.Count; i++)
        {
            var step = ParseStep(stepsJson[i], id, i, stepIds);
            if (step != null) steps.Add(step);
        }

        if (steps.Count == 0) return null;
        return new TutorialChapter(data, id, ParseIcon(data.Icon, null), steps, ParseRewards(data.Reward));
    }

    private TutorialStep ParseStep(TutorialStepJSON data, string chapterId, int index, HashSet<string> stepIds)
    {
        if (data == null) return null;

        string id = string.IsNullOrWhiteSpace(data.Id) ? chapterId + "." + (index + 1) : data.Id;
        if (!stepIds.Add(id))
        {
            Warn($"duplicate step id '{id}', the step is ignored");
            return null;
        }

        var tasks = new List<TutorialTaskDefinition>();
        foreach (var taskJson in data.Tasks ?? new List<TutorialTaskJSON>())
        {
            var task = ParseTask(taskJson, id);
            if (task != null) tasks.Add(task);
        }

        GameMode? mode = null;
        if (!string.IsNullOrWhiteSpace(data.RequiresGameMode))
        {
            if (Enum.TryParse(data.RequiresGameMode, true, out GameMode parsed)) mode = parsed;
            else Warn($"step '{id}': unknown game mode '{data.RequiresGameMode}'");
        }

        return new TutorialStep(data, id, ParseIcon(data.Icon, null), tasks, ParseRewards(data.Reward), mode);
    }

    private TutorialTaskDefinition ParseTask(TutorialTaskJSON data, string stepId)
    {
        if (data == null) return null;

        string type = (data.Type ?? "").Trim().ToLowerInvariant();
        if (!TutorialTaskFactory.IsKnown(type))
        {
            Warn($"step '{stepId}': unknown task type '{data.Type}'");
            return null;
        }

        string itemKey = !string.IsNullOrWhiteSpace(data.Item) ? data.Item.Trim() : (data.Block ?? "").Trim();
        short itemId = 0;
        if (itemKey.Length > 0 && !_items.TryResolve(itemKey, out itemId))
        {
            Warn($"step '{stepId}': unknown item '{itemKey}' in a '{type}' task");
            return null;
        }

        if ((type == "have_item" || type == "select_hotbar") && itemId == 0)
        {
            Warn($"step '{stepId}': '{type}' needs an Item");
            return null;
        }

        if ((type == "press" || type == "hold") && string.IsNullOrWhiteSpace(data.Action))
        {
            Warn($"step '{stepId}': '{type}' needs an Action");
            return null;
        }

        var children = new List<TutorialTaskDefinition>();
        if (type == "all_of" || type == "any_of")
        {
            foreach (var childJson in data.Tasks ?? new List<TutorialTaskJSON>())
            {
                var child = ParseTask(childJson, stepId);
                if (child != null) children.Add(child);
            }

            if (children.Count == 0)
            {
                Warn($"step '{stepId}': '{type}' has no valid tasks");
                return null;
            }
        }

        return new TutorialTaskDefinition(data, type, itemId, itemId != 0 ? itemKey : "", ParseIcon(data.Icon, itemId), children);
    }

    private TutorialIcon ParseIcon(string icon, short? fallbackItem)
    {
        if (!string.IsNullOrWhiteSpace(icon))
        {
            if (icon.StartsWith(UiIconPrefix, StringComparison.OrdinalIgnoreCase)) return new TutorialIcon(0, icon.Substring(UiIconPrefix.Length));
            if (_items.TryResolve(icon, out short iconItem)) return new TutorialIcon(iconItem, null);
            Warn($"unknown icon '{icon}'");
        }

        return fallbackItem is > 0 ? new TutorialIcon(fallbackItem.Value, null) : TutorialIcon.None;
    }

    private List<TutorialReward> ParseRewards(List<TutorialRewardJSON> rewards)
    {
        var result = new List<TutorialReward>();
        if (rewards == null) return result;

        foreach (var reward in rewards)
        {
            if (reward == null || reward.Count <= 0) continue;
            if (_items.TryResolve(reward.Item, out short itemId)) result.Add(new TutorialReward(itemId, Math.Min(reward.Count, 999)));
            else Warn($"unknown reward item '{reward.Item}'");
        }

        return result;
    }

    private HashSet<GameMode> ParseModes(List<string> modes)
    {
        var result = new HashSet<GameMode>();
        if (modes == null) return result;

        foreach (var mode in modes)
        {
            if (Enum.TryParse(mode, true, out GameMode parsed)) result.Add(parsed);
            else Warn($"unknown game mode '{mode}'");
        }

        return result;
    }

    private void Warn(string message)
    {
        _warnings?.Add($"[Tutorial] {_source}: {message}");
    }
}
