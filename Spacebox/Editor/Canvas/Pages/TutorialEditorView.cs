using System.Text.Json.Nodes;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Tutorial;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class TutorialEditorView : EntryView, IEntryEditor
{
    private const float CheckInterval = 0.25f;

    private readonly FieldWidgetRegistry _widgets;
    private readonly List<string> _warnings = new List<string>();
    private readonly VBoxContainer _root = new VBoxContainer { Name = "TutorialEditor" };
    private readonly Label _status = new Label { Autowrap = true, MouseFilter = MouseFilter.Stop };
    private readonly ItemList _outline = new ItemList { Name = "Outline", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Label _nodeTitle = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly VBoxContainer _card = new VBoxContainer { Name = "Card", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly ConfirmDialog _confirm = new ConfirmDialog();
    private JsonDocumentView _document;
    private Button _moveUp;
    private Button _moveDown;
    private Button _duplicate;
    private Button _delete;
    private EditorContext _editor;
    private EditorEntry _entry;
    private TutorialOutline _model;
    private TutorialDefinition _definition;
    private TutorialNode _selected = TutorialNode.Settings;
    private string _outlineSignature;
    private string _parsedJson;
    private float _sinceCheck;

    public TutorialEditorView(FieldWidgetRegistry widgets)
    {
        _widgets = widgets;
    }

    protected override Control Create()
    {
        _root.AddChild(_status);
        var split = _root.AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 200f, MinSecond = 300f });
        split.Ratio = 0.32f;

        var left = split.AddChild(new VBoxContainer());
        var add = left.AddChild(new HBoxContainer());
        Action(add, "+ Chapter", "Adds a new chapter after the selected one.", () => Select(_model.AddChapter(_selected), true));
        Action(add, "+ Step", "Adds a new step after the selected step, or at the end of the selected chapter.", () => Select(_model.AddStep(_selected), true));
        var edit = left.AddChild(new HBoxContainer());
        _moveUp = Action(edit, "Up", "Moves the selected chapter or step one place up.", () => Select(_model.Move(_selected, -1), true));
        _moveDown = Action(edit, "Down", "Moves the selected chapter or step one place down.", () => Select(_model.Move(_selected, 1), true));
        _duplicate = Action(edit, "Duplicate", "Copies the selected chapter or step.", () => Select(_model.Duplicate(_selected), true));
        _delete = Action(edit, "Delete", "Deletes the selected chapter or step (asks first).", AskDelete);
        left.AddChild(_outline);
        _outline.ItemSelected += index =>
        {
            if (_outline.Items[index].Metadata is TutorialNode node) Select(node, false);
        };

        var right = split.AddChild(new SplitContainer(true) { MinFirst = 160f, MinSecond = 80f });
        right.Ratio = 0.62f;
        var top = right.AddChild(new VBoxContainer());
        top.AddChild(_nodeTitle);
        _document = top.AddChild(new JsonDocumentView(_widgets) { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand });
        var scroll = right.AddChild(new ScrollContainer());
        scroll.AddChild(_card);
        return _root;
    }

    public override void Show(EditorContext editor, EditorEntry entry)
    {
        _editor = editor;
        bool sameTutorial = _entry != null && ReferenceEquals(_entry.Node, entry.Node);
        _entry = entry;
        _model = entry.Node is JsonObject obj ? new TutorialOutline(obj) : null;
        if (!sameTutorial) _selected = TutorialNode.Settings;
        _outlineSignature = null;
        _parsedJson = null;
        Refresh();
        Select(_selected, false);
    }

    public override void Tick(EditorContext editor, float deltaTime)
    {
        if (_model == null) return;
        _sinceCheck += deltaTime;
        if (_sinceCheck < CheckInterval) return;
        _sinceCheck = 0f;
        Refresh();
    }

    private void Refresh()
    {
        if (_model == null) return;
        string signature = _model.Signature();
        if (signature != _outlineSignature)
        {
            _outlineSignature = signature;
            if (!_model.Exists(_selected)) _selected = TutorialNode.Settings;
            RebuildOutline();
            UpdateButtons();
        }

        string json = _entry.Node.ToJsonString();
        if (json == _parsedJson) return;
        _parsedJson = json;
        _warnings.Clear();
        _definition = new TutorialParser(new EditorItemResolver(_editor), _warnings).Parse(json, _entry.File.RelativePath);
        ShowStatus();
        ShowCard();
    }

    private void Select(TutorialNode node, bool structural)
    {
        if (_model == null) return;
        if (!_model.Exists(node)) node = TutorialNode.Settings;
        _selected = node;
        if (structural)
        {
            _entry.File.MarkDirty();
            _outlineSignature = null;
            Refresh();
        }

        int index = _outline.IndexOf(item => item.Metadata is TutorialNode n && n == node);
        if (index >= 0 && index != _outline.SelectedIndex) _outline.Select(index);
        if (index >= 0) _outline.EnsureVisible(index);

        _document.HiddenRootKeys.Clear();
        Type schema = typeof(TutorialJSON);
        if (node.Kind == TutorialNodeKind.Tutorial) _document.HiddenRootKeys.Add("Chapters");
        else if (node.Kind == TutorialNodeKind.Chapter)
        {
            schema = typeof(TutorialChapterJSON);
            _document.HiddenRootKeys.Add("Steps");
        }
        else schema = typeof(TutorialStepJSON);

        _nodeTitle.Text = NodeTitle(node);
        _document.BindNode(_editor, _entry.File, _model.Get(node), schema, EditorScopes.Tutorials);
        UpdateButtons();
        ShowCard();
    }

    private string NodeTitle(TutorialNode node)
    {
        return node.Kind switch
        {
            TutorialNodeKind.Tutorial => "Tutorial settings (name, game modes, order)",
            TutorialNodeKind.Chapter => "Chapter " + _model.Label(node),
            _ => $"Chapter {node.Chapter + 1}, step " + _model.Label(node)
        };
    }

    private void RebuildOutline()
    {
        _outline.Clear();
        foreach (var node in _model.Nodes())
        {
            string label = _model.Label(node);
            ListItem item;
            if (node.Kind == TutorialNodeKind.Chapter)
            {
                item = _outline.AddItem(label, null, node);
                item.TextColor = EditorCanvasTheme.Accent;
            }
            else
            {
                item = _outline.AddItem(label, null, node);
                if (node.Kind == TutorialNodeKind.Step) item.Indent = 1;
            }
            if (node == _selected) _outline.Select(_outline.Items.Count - 1);
        }
    }

    private void UpdateButtons()
    {
        if (_moveUp == null || _model == null) return;
        bool editable = _selected.Kind != TutorialNodeKind.Tutorial;
        _moveUp.Disabled = !_model.CanMove(_selected, -1);
        _moveDown.Disabled = !_model.CanMove(_selected, 1);
        _duplicate.Disabled = !editable;
        _delete.Disabled = !editable;
    }

    private void AskDelete()
    {
        if (_selected.Kind == TutorialNodeKind.Tutorial) return;
        var node = _selected;
        string what = node.Kind == TutorialNodeKind.Chapter ? "chapter and all its steps" : "step";
        _confirm.Show(_root.Root, "Delete " + (node.Kind == TutorialNodeKind.Chapter ? "chapter" : "step"), $"Delete the {what} '{_model.Label(node)}'? Files are only changed when you save.", new[] { "Delete", "Cancel" }, result =>
        {
            if (result == 0) Select(_model.Remove(node), true);
        });
    }

    private void ShowStatus()
    {
        _status.TooltipText = "Checked with the same parser the game uses: task types, item ids, icons and game modes.";
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
            return;
        }

        _status.Text = $"{_warnings.Count} problems (the game logs the same warnings):\n- " + string.Join("\n- ", _warnings);
        _status.FontColor = EditorCanvasTheme.Warning;
    }

    private void ShowCard()
    {
        _card.ClearChildren();
        if (_definition == null || _model == null) return;

        if (_selected.Kind == TutorialNodeKind.Tutorial)
        {
            _card.AddChild(Dim($"{_definition.Data.Name}  ({string.Join(", ", _definition.Data.GameModes)}), {_definition.Chapters.Count} chapters"));
            return;
        }

        if (_selected.Chapter >= _definition.Chapters.Count)
        {
            _card.AddChild(Dim("No preview: the game skips this chapter (see the problems above)."));
            return;
        }

        var chapter = _definition.Chapters[_selected.Chapter];
        if (_selected.Kind == TutorialNodeKind.Chapter)
        {
            _card.AddChild(Dim($"{chapter.Steps.Count} steps, reward: {Rewards(chapter.Rewards)}{(chapter.Optional ? ", optional" : "")}"));
            return;
        }

        if (_selected.Step >= chapter.Steps.Count)
        {
            _card.AddChild(Dim("No preview: the game skips this step (see the problems above)."));
            return;
        }

        var step = chapter.Steps[_selected.Step];
        var resolver = new EditorTextResolver(_editor);
        _card.AddChild(Dim($"Preview  -  {chapter.Title}, step {_selected.Step + 1}/{chapter.Steps.Count}"));
        var title = _card.AddChild(new HBoxContainer());
        if (step.Icon.IsItem && GameAssets.Items.TryGetValue(step.Icon.ItemId, out var iconItem)) title.AddChild(CanvasIcons.Image(iconItem.Icon, 28f));
        title.AddChild(new Label(step.Title) { FontColor = ToColor(TutorialDraw.Gold), VerticalAlignment = VerticalAlignment.Center });
        _card.AddChild(new HSeparator());
        _card.AddChild(Wrapped(TutorialText.Format(step.Text, resolver), TutorialDraw.TextBright));
        if (!string.IsNullOrWhiteSpace(step.Hint)) _card.AddChild(Wrapped("Hint: " + TutorialText.Format(step.Hint, resolver), TutorialDraw.Cyan));
        foreach (var task in step.Tasks) AddTask(task, resolver, 0);

        _card.AddChild(Dim($"Reward: {Rewards(step.Rewards)}"));
        var details = new List<string>();
        if (step.Delay > 0f) details.Add($"appears after {step.Delay:0.#}s");
        if (step.Tasks.Count == 0) details.Add($"completes after {step.ReadSeconds:0.#}s of reading");
        if (!step.Skippable) details.Add("cannot be skipped");
        if (step.RequiredMode != null) details.Add($"only in {step.RequiredMode}");
        if (details.Count > 0) _card.AddChild(Dim(string.Join(", ", details)));
    }

    private void AddTask(TutorialTaskDefinition task, ITutorialTextResolver resolver, int depth)
    {
        var label = Wrapped(new string(' ', depth * 4) + "[ ] " + TutorialText.Format(task.Label, resolver, task.Data.Count), task.Optional ? TutorialDraw.TextFaint : TutorialDraw.TextDim);
        label.TooltipText = $"Task type: {task.Type}{(task.Optional ? ", optional" : "")}";
        label.MouseFilter = MouseFilter.Stop;
        _card.AddChild(label);
        foreach (var child in task.Children) AddTask(child, resolver, depth + 1);
    }

    private static Button Action(Container parent, string text, string tooltip, Action action)
    {
        var button = parent.AddChild(new Button(text) { TooltipText = tooltip, ThemeTypeVariation = "SmallButton" });
        button.Pressed += _ => action();
        return button;
    }

    private static Label Dim(string text) => new Label(text) { Autowrap = true, FontColor = EditorCanvasTheme.TextDim, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };

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
