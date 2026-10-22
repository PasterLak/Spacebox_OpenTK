using System.Numerics;
using ImGuiNET;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Editor.UI.Panels;

public sealed class TutorialEntryPreview : IEntryCustomPreview
{
    private readonly List<string> _warnings = new List<string>();
    private TutorialDefinition _definition;
    private string _parsedJson;
    private int _chapter;
    private int _step;

    public bool CanPreview(EditorContext context, EditorEntry entry) => entry.Node != null;

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
    }

    public void Draw(EditorContext context, EditorEntry entry)
    {
        Parse(context, entry);
        if (_definition == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.3f, 1f), "The tutorial cannot be read:");
            foreach (var warning in _warnings) ImGui.BulletText(warning);
            return;
        }

        DrawWarnings();
        var available = ImGui.GetContentRegionAvail();
        if (ImGui.BeginChild("##tutorialOutline", new Vector2(available.X * 0.38f, 0f), ImGuiChildFlags.Border))
            DrawOutline(context);
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##tutorialCard", Vector2.Zero, ImGuiChildFlags.Border))
            DrawCard(context);
        ImGui.EndChild();
    }

    private void Parse(EditorContext context, EditorEntry entry)
    {
        string json = entry.Node.ToJsonString();
        if (json == _parsedJson) return;

        _parsedJson = json;
        _warnings.Clear();
        _definition = new TutorialParser(new EditorItemResolver(context), _warnings).Parse(json, entry.File.RelativePath);
        _chapter = Math.Clamp(_chapter, 0, Math.Max(0, (_definition?.Chapters.Count ?? 1) - 1));
        _step = 0;
    }

    private void DrawWarnings()
    {
        if (_warnings.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.45f, 0.85f, 0.45f, 1f), "No problems found.");
            EditorWidgets.Tooltip("Checked with the same parser the game uses: task types, item ids, icons and game modes.");
            return;
        }

        ImGui.TextColored(new Vector4(1f, 0.75f, 0.3f, 1f), $"{_warnings.Count} problems (the game logs the same warnings):");
        foreach (var warning in _warnings) ImGui.BulletText(warning);
    }

    private void DrawOutline(EditorContext context)
    {
        ImGui.TextDisabled($"{_definition.Data.Name}  ({string.Join(", ", _definition.Data.GameModes)})");
        for (int c = 0; c < _definition.Chapters.Count; c++)
        {
            var chapter = _definition.Chapters[c];
            ImGui.PushID(c);
            var flags = ImGuiTreeNodeFlags.SpanFullWidth | (c == _chapter ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
            bool open = ImGui.TreeNodeEx($"{c + 1}. {chapter.Title}{(chapter.Optional ? " (optional)" : "")}##chapter", flags);
            EditorWidgets.Tooltip($"{chapter.Steps.Count} steps, reward: {Rewards(context, chapter.Rewards)}");
            if (open)
            {
                for (int s = 0; s < chapter.Steps.Count; s++)
                {
                    if (ImGui.Selectable($"{s + 1}. {chapter.Steps[s].Title}##step{s}", c == _chapter && s == _step))
                    {
                        _chapter = c;
                        _step = s;
                    }
                }
                ImGui.TreePop();
            }
            ImGui.PopID();
        }
    }

    private void DrawCard(EditorContext context)
    {
        if (_chapter >= _definition.Chapters.Count || _step >= _definition.Chapters[_chapter].Steps.Count)
        {
            ImGui.TextDisabled("Select a step on the left.");
            return;
        }

        var chapter = _definition.Chapters[_chapter];
        var step = chapter.Steps[_step];
        var resolver = new EditorTextResolver(context);
        float iconSize = ImGui.GetTextLineHeight() * 1.8f;

        ImGui.TextDisabled($"{chapter.Title}  -  step {_step + 1}/{chapter.Steps.Count}");
        DrawIcon(context, step.Icon, iconSize);
        ImGui.SameLine();
        ImGui.TextColored(ToVector(TutorialDraw.Gold), step.Title);
        ImGui.Separator();
        ImGui.PushTextWrapPos(0f);
        ImGui.TextColored(ToVector(TutorialDraw.TextBright), TutorialText.Format(step.Text, resolver));
        if (!string.IsNullOrWhiteSpace(step.Hint)) ImGui.TextColored(ToVector(TutorialDraw.Cyan), "Hint: " + TutorialText.Format(step.Hint, resolver));
        ImGui.PopTextWrapPos();

        ImGui.Spacing();
        foreach (var task in step.Tasks) DrawTask(context, task, resolver, 0);

        ImGui.Spacing();
        ImGui.TextDisabled($"Reward: {Rewards(context, step.Rewards)}");
        var details = new List<string>();
        if (step.Delay > 0f) details.Add($"appears after {step.Delay:0.#}s");
        if (step.Tasks.Count == 0) details.Add($"completes after {step.ReadSeconds:0.#}s of reading");
        if (!step.Skippable) details.Add("cannot be skipped");
        if (step.RequiredMode != null) details.Add($"only in {step.RequiredMode}");
        if (details.Count > 0) ImGui.TextDisabled(string.Join(", ", details));
    }

    private static void DrawTask(EditorContext context, TutorialTaskDefinition task, ITutorialTextResolver resolver, int depth)
    {
        ImGui.Indent(depth * 16f);
        ImGui.TextColored(ToVector(task.Optional ? TutorialDraw.TextFaint : TutorialDraw.TextDim), "[ ] " + TutorialText.Format(task.Label, resolver, task.Data.Count));
        EditorWidgets.Tooltip($"Task type: {task.Type}{(task.Optional ? ", optional" : "")}");
        ImGui.Unindent(depth * 16f);
        foreach (var child in task.Children) DrawTask(context, child, resolver, depth + 1);
    }

    private static void DrawIcon(EditorContext context, TutorialIcon icon, float size)
    {
        if (icon.IsItem && GameAssets.Items.TryGetValue(icon.ItemId, out var item)) EditorWidgets.Icon(item.Icon, size);
        else ImGui.Dummy(new Vector2(size, size));
    }

    private static string Rewards(EditorContext context, IReadOnlyList<TutorialReward> rewards)
    {
        if (rewards.Count == 0) return "none";

        var names = new List<string>();
        foreach (var reward in rewards)
            names.Add(GameAssets.Items.TryGetValue(reward.ItemId, out var item) ? $"{reward.Count}x {item.Name}" : $"{reward.Count}x #{reward.ItemId}");
        return string.Join(", ", names);
    }

    private static Vector4 ToVector(uint color)
    {
        return new Vector4((color & 0xFF) / 255f, ((color >> 8) & 0xFF) / 255f, ((color >> 16) & 0xFF) / 255f, ((color >> 24) & 0xFF) / 255f);
    }

    private sealed class EditorItemResolver : ITutorialItemResolver
    {
        private readonly EditorContext _context;

        public EditorItemResolver(EditorContext context) => _context = context;

        public bool TryResolve(string id, out short itemId)
        {
            itemId = 0;
            if (!_context.Assets.TryGetItem(id, out var item)) return false;
            itemId = item.Id;
            return true;
        }
    }

    private sealed class EditorTextResolver : ITutorialTextResolver
    {
        private readonly EditorContext _context;

        public EditorTextResolver(EditorContext context) => _context = context;

        public string KeyName(string action) => KeyNames.ForAction(action);

        public string ItemName(string itemKey) => _context.Assets.TryGetItem(itemKey, out var item) ? item.Name : null;
    }
}
