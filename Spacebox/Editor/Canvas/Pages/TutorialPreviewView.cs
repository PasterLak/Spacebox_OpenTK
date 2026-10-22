using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class TutorialPreviewView : RefreshingEntryView
{
    private readonly List<string> _warnings = new List<string>();
    private readonly VBoxContainer _root = new VBoxContainer { Name = "Tutorial" };
    private readonly Label _status = new Label { Autowrap = true };
    private readonly ItemList _outline = new ItemList { Name = "Outline", IconSize = 16f, SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly VBoxContainer _card = new VBoxContainer { Name = "Card" };
    private TutorialDefinition _definition;
    private EditorContext _editor;
    private int _chapter;
    private int _step;

    protected override Control Create()
    {
        _root.AddChild(_status);
        var split = _root.AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 160f, MinSecond = 200f });
        split.Ratio = 0.38f;
        split.AddChild(_outline);
        var scroll = split.AddChild(new ScrollContainer());
        scroll.AddChild(_card);
        _card.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        _outline.ItemSelected += index =>
        {
            if (_outline.Items[index].Metadata is not (int chapter, int step)) return;
            _chapter = chapter;
            _step = step;
            ShowCard();
        };
        return _root;
    }

    protected override void Rebuild(EditorContext editor, EditorEntry entry)
    {
        _editor = editor;
        _warnings.Clear();
        _definition = new TutorialParser(new EditorItemResolver(editor), _warnings).Parse(entry.Node.ToJsonString(), entry.File.RelativePath);
        ShowStatus();
        ShowOutline();
        ShowCard();
    }

    private void ShowStatus()
    {
        if (_definition == null)
        {
            _status.Text = "The tutorial cannot be read:\n- " + string.Join("\n- ", _warnings);
            _status.FontColor = EditorCanvasTheme.Error;
            return;
        }

        if (_warnings.Count == 0)
        {
            _status.Text = "No problems found.";
            _status.FontColor = EditorCanvasTheme.Success;
            _status.TooltipText = "Checked with the same parser the game uses: task types, item ids, icons and game modes.";
            return;
        }

        _status.Text = $"{_warnings.Count} problems (the game logs the same warnings):\n- " + string.Join("\n- ", _warnings);
        _status.FontColor = EditorCanvasTheme.Warning;
    }

    private void ShowOutline()
    {
        _outline.Clear();
        if (_definition == null) return;

        _chapter = Math.Clamp(_chapter, 0, Math.Max(0, _definition.Chapters.Count - 1));
        _outline.AddHeader($"{_definition.Data.Name}  ({string.Join(", ", _definition.Data.GameModes)})");
        for (int c = 0; c < _definition.Chapters.Count; c++)
        {
            var chapter = _definition.Chapters[c];
            var header = _outline.AddHeader($"{c + 1}. {chapter.Title}{(chapter.Optional ? " (optional)" : "")}");
            header.Tooltip = $"{chapter.Steps.Count} steps, reward: {Rewards(chapter.Rewards)}";
            for (int s = 0; s < chapter.Steps.Count; s++)
            {
                var item = _outline.AddItem($"{s + 1}. {chapter.Steps[s].Title}", null, (c, s));
                item.Indent = 1;
                if (c == _chapter && s == _step) _outline.Select(_outline.Items.Count - 1);
            }
        }
    }

    private void ShowCard()
    {
        _card.ClearChildren();
        if (_definition == null || _chapter >= _definition.Chapters.Count || _step >= _definition.Chapters[_chapter].Steps.Count)
        {
            _card.AddChild(new Label("Select a step on the left.") { FontColor = EditorCanvasTheme.TextDim });
            return;
        }

        var chapter = _definition.Chapters[_chapter];
        var step = chapter.Steps[_step];
        var resolver = new EditorTextResolver(_editor);

        _card.AddChild(new Label($"{chapter.Title}  -  step {_step + 1}/{chapter.Steps.Count}") { FontColor = EditorCanvasTheme.TextDim });
        var title = _card.AddChild(new HBoxContainer());
        if (step.Icon.IsItem && GameAssets.Items.TryGetValue(step.Icon.ItemId, out var iconItem)) title.AddChild(CanvasIcons.Image(iconItem.Icon, 28f));
        title.AddChild(new Label(step.Title) { FontColor = ToColor(TutorialDraw.Gold), VerticalAlignment = VerticalAlignment.Center });
        _card.AddChild(new HSeparator());
        _card.AddChild(Wrapped(TutorialText.Format(step.Text, resolver), TutorialDraw.TextBright));
        if (!string.IsNullOrWhiteSpace(step.Hint)) _card.AddChild(Wrapped("Hint: " + TutorialText.Format(step.Hint, resolver), TutorialDraw.Cyan));

        foreach (var task in step.Tasks) AddTask(task, resolver, 0);

        _card.AddChild(new Label($"Reward: {Rewards(step.Rewards)}") { FontColor = EditorCanvasTheme.TextDim });
        var details = new List<string>();
        if (step.Delay > 0f) details.Add($"appears after {step.Delay:0.#}s");
        if (step.Tasks.Count == 0) details.Add($"completes after {step.ReadSeconds:0.#}s of reading");
        if (!step.Skippable) details.Add("cannot be skipped");
        if (step.RequiredMode != null) details.Add($"only in {step.RequiredMode}");
        if (details.Count > 0) _card.AddChild(new Label(string.Join(", ", details)) { FontColor = EditorCanvasTheme.TextDim });
    }

    private void AddTask(TutorialTaskDefinition task, ITutorialTextResolver resolver, int depth)
    {
        var label = Wrapped(new string(' ', depth * 4) + "[ ] " + TutorialText.Format(task.Label, resolver, task.Data.Count), task.Optional ? TutorialDraw.TextFaint : TutorialDraw.TextDim);
        label.TooltipText = $"Task type: {task.Type}{(task.Optional ? ", optional" : "")}";
        label.MouseFilter = MouseFilter.Stop;
        _card.AddChild(label);
        foreach (var child in task.Children) AddTask(child, resolver, depth + 1);
    }

    private static Label Wrapped(string text, uint color) => new Label(text) { Autowrap = true, FontColor = ToColor(color), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };

    private static string Rewards(IReadOnlyList<TutorialReward> rewards)
    {
        if (rewards.Count == 0) return "none";

        var names = new List<string>();
        foreach (var reward in rewards)
            names.Add(GameAssets.Items.TryGetValue(reward.ItemId, out var item) ? $"{reward.Count}x {item.Name}" : $"{reward.Count}x #{reward.ItemId}");
        return string.Join(", ", names);
    }

    private static Color4 ToColor(uint color)
    {
        return new Color4((color & 0xFF) / 255f, ((color >> 8) & 0xFF) / 255f, ((color >> 16) & 0xFF) / 255f, ((color >> 24) & 0xFF) / 255f);
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
