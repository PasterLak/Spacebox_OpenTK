using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Tutorial;

public readonly struct TutorialIcon
{
    public static readonly TutorialIcon None = default;

    public readonly short ItemId;
    public readonly string UiName;

    public TutorialIcon(short itemId, string uiName)
    {
        ItemId = itemId;
        UiName = uiName;
    }

    public bool IsItem => ItemId > 0;
    public bool IsUi => !string.IsNullOrEmpty(UiName);
    public bool IsEmpty => !IsItem && !IsUi;
}

public readonly struct TutorialReward
{
    public readonly short ItemId;
    public readonly int Count;

    public TutorialReward(short itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }
}

public sealed class TutorialTaskDefinition
{
    public TutorialTaskDefinition(TutorialTaskJSON data, string type, short itemId, string itemKey, TutorialIcon icon, IReadOnlyList<TutorialTaskDefinition> children)
    {
        Data = data;
        Type = type;
        ItemId = itemId;
        ItemKey = itemKey;
        Icon = icon;
        Children = children;
    }

    public TutorialTaskJSON Data { get; }
    public string Type { get; }
    public short ItemId { get; }
    public string ItemKey { get; }
    public TutorialIcon Icon { get; }
    public IReadOnlyList<TutorialTaskDefinition> Children { get; }
    public bool Optional => Data.Optional;
    public string Label => string.IsNullOrWhiteSpace(Data.Label) ? TutorialTaskLabels.Default(this) : Data.Label;
}

public sealed class TutorialStep
{
    public TutorialStep(TutorialStepJSON data, string id, TutorialIcon icon, IReadOnlyList<TutorialTaskDefinition> tasks, IReadOnlyList<TutorialReward> rewards, GameMode? requiredMode)
    {
        Data = data;
        Id = id;
        Icon = icon;
        Tasks = tasks;
        Rewards = rewards;
        RequiredMode = requiredMode;
    }

    public TutorialStepJSON Data { get; }
    public string Id { get; }
    public TutorialIcon Icon { get; }
    public IReadOnlyList<TutorialTaskDefinition> Tasks { get; }
    public IReadOnlyList<TutorialReward> Rewards { get; }
    public GameMode? RequiredMode { get; }
    public string Title => Data.Title;
    public string Text => Data.Text;
    public string Hint => Data.Hint;
    public bool Skippable => Data.Skippable;
    public float Delay => Math.Max(0f, Data.Delay);
    public float ReadSeconds => Math.Max(0.5f, Data.ReadSeconds);

    public bool IsAvailableIn(GameMode mode)
    {
        return RequiredMode == null || RequiredMode == mode;
    }
}

public sealed class TutorialChapter
{
    public TutorialChapter(TutorialChapterJSON data, string id, TutorialIcon icon, IReadOnlyList<TutorialStep> steps, IReadOnlyList<TutorialReward> rewards)
    {
        Data = data;
        Id = id;
        Icon = icon;
        Steps = steps;
        Rewards = rewards;
    }

    public TutorialChapterJSON Data { get; }
    public string Id { get; }
    public TutorialIcon Icon { get; }
    public IReadOnlyList<TutorialStep> Steps { get; }
    public IReadOnlyList<TutorialReward> Rewards { get; }
    public string Title => Data.Title;
    public bool Optional => Data.Optional;
}

public sealed class TutorialDefinition
{
    public TutorialDefinition(TutorialJSON data, IReadOnlyList<TutorialChapter> chapters, IReadOnlySet<GameMode> gameModes)
    {
        Data = data;
        Chapters = chapters;
        GameModes = gameModes;
    }

    public TutorialJSON Data { get; }
    public IReadOnlyList<TutorialChapter> Chapters { get; }
    public IReadOnlySet<GameMode> GameModes { get; }
    public string Id => Data.Id;
    public string Name => Data.Name;
    public int Order => Data.Order;

    public bool SupportsMode(GameMode mode)
    {
        return GameModes.Count == 0 || GameModes.Contains(mode);
    }

    public bool TryFindStep(string stepId, out int chapterIndex, out int stepIndex)
    {
        for (chapterIndex = 0; chapterIndex < Chapters.Count; chapterIndex++)
        {
            var steps = Chapters[chapterIndex].Steps;
            for (stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                if (steps[stepIndex].Id == stepId) return true;
            }
        }

        chapterIndex = -1;
        stepIndex = -1;
        return false;
    }
}
