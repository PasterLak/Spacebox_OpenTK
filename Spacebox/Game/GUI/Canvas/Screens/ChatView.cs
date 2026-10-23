using Engine.CanvasUI;
using Engine.Commands;
using Engine.UI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class ChatView : GameScreen
{
    public const float WidthShare = 1f / 3f;
    public const float HeightShare = 0.5f;
    public const int MaxSuggestions = 8;
    public const string HelpText = "Tab completes, Up/Down selects";
    private const string OpenPanel = "ChatPanel";
    private const string ClosedPanel = "ChatPanelClear";
    private const float SuggestionGap = 4f;
    private const float EdgeMargin = 6f;

    private static readonly Color4 ShadowColor = new Color4(0f, 0f, 0f, 0.85f);

    private readonly SafeArea _area = new SafeArea { Name = "ChatArea", MouseFilter = MouseFilter.Ignore };
    private readonly PanelContainer _box = new PanelContainer { Name = "ChatBox", ThemeTypeVariation = ClosedPanel, MouseFilter = MouseFilter.Ignore };
    private readonly VBoxContainer _column = new VBoxContainer { MouseFilter = MouseFilter.Ignore };
    private readonly ChatScroll _scroll = new ChatScroll { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilter.Ignore };
    private readonly VBoxContainer _lines = new VBoxContainer { Alignment = BoxAlignment.End, MouseFilter = MouseFilter.Ignore };
    private readonly ConsoleInput _input = new ConsoleInput { Name = "ChatInput", MaxLength = ChatController.MaxInputLength, Visible = false, PlaceholderText = "Message or /command" };
    private readonly PanelContainer _suggestionPanel = new PanelContainer { Name = "ChatSuggestions", ThemeTypeVariation = "PopupPanel", MouseFilter = MouseFilter.Ignore, Visible = false };
    private readonly VBoxContainer _suggestionList = new VBoxContainer { MouseFilter = MouseFilter.Ignore };
    private readonly Label _usage = new Label { ThemeTypeVariation = "MutedLabel", Autowrap = true, MouseFilter = MouseFilter.Ignore };
    private readonly Label _help = new Label { Text = HelpText, ThemeTypeVariation = "MutedLabel", Autowrap = true, MouseFilter = MouseFilter.Ignore };
    private readonly List<Label> _labels = new List<Label>();
    private readonly List<SuggestionRow> _rows = new List<SuggestionRow>();
    private ChatLog _log;
    private int _logVersion = -1;
    private int _inputRevision = -1;
    private int _suggestionVersion = -1;
    private int _suggestionStart = -1;
    private int _selected = -1;
    private bool _typing;
    private bool _onLeft = true;
    private bool _syncingInput;

    private readonly ChatController _chat;
    private readonly WorldLoadingController _loading;

    public ChatView(ChatController chat, WorldLoadingController loading)
    {
        _chat = chat;
        _loading = loading;
        Name = "Chat";
        _area.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_area);
        _area.AddChild(_box);
        _area.AddChild(_suggestionPanel);

        _column.AddThemeConstantOverride("separation", 4f);
        _box.AddChild(_column);
        _column.AddChild(_scroll);
        _scroll.AddChild(_lines);
        _lines.AddThemeConstantOverride("separation", 2f);
        _column.AddChild(_input);

        _suggestionPanel.AddChild(_suggestionList);
        _suggestionList.AddThemeConstantOverride("separation", 2f);
        _suggestionList.AddChild(_usage);
        _suggestionList.AddChild(_help);

        _input.TextChanged += text =>
        {
            if (!_syncingInput) _chat.SetInput(text);
        };
        _input.TextSubmitted += text =>
        {
            _input.ReleaseFocus();
            _chat.Submit(text);
        };
        _input.CompletionRequested += _chat.Complete;
        _input.HistoryRequested += step => _chat.Navigate(step < 0);
        PlaceBox();
    }

    public override bool IsShown => _input.HasFocus || (_chat.IsVisible && !_loading.IsActive);

    public override bool WantsInput => _chat.IsTyping;

    public bool IsTypingShown => _typing;

    public Control InputField => _input;

    protected override void Refresh(float deltaTime)
    {
        bool typing = _chat.IsTyping && !_loading.IsActive;
        Present(_chat.Log, Engine.Time.Total, _chat.HideDelay, typing, _chat.PlaceOnLeft);
        if (!typing) return;

        SyncInput(_chat.InputText, _chat.InputRevision);
        var commands = _chat.Commands;
        PresentSuggestions(commands.Usage, commands.Suggestions, commands.Selected, commands.Version);
    }

    public void Present(ChatLog log, float now, float hideDelay, bool typing, bool onLeft)
    {
        if (onLeft != _onLeft)
        {
            _onLeft = onLeft;
            PlaceBox();
        }

        if (typing != _typing) SetTyping(typing);
        if (!ReferenceEquals(log, _log) || log.Version != _logVersion) SyncLines(log);

        var messages = log.Messages;
        for (int i = 0; i < _labels.Count; i++)
        {
            var label = _labels[i];
            float opacity = typing ? 1f : ChatLog.Opacity(now - messages[i].Time, hideDelay);
            bool show = opacity > 0f;
            if (label.Visible != show) label.Visible = show;
            if (show && label.Modulate.A != opacity) label.Modulate = new Color4(1f, 1f, 1f, opacity);
        }

        if (!typing || _scroll.StickToBottom) _scroll.ScrollOffset = _scroll.MaxScroll;
    }

    public void SyncInput(string text, int revision)
    {
        if (revision != _inputRevision)
        {
            _inputRevision = revision;
            _syncingInput = true;
            _input.SetTextAtEnd(text);
            _syncingInput = false;
        }

        if (!_input.HasFocus) _input.GrabFocus();
    }

    public void PresentSuggestions(string usage, IReadOnlyList<CommandSuggestion> suggestions, int selected, int version)
    {
        int count = Math.Min(suggestions.Count, MaxSuggestions);
        int start = Math.Clamp(selected - MaxSuggestions + 1, 0, Math.Max(0, suggestions.Count - MaxSuggestions));
        if (version != _suggestionVersion || start != _suggestionStart)
        {
            _suggestionVersion = version;
            _suggestionStart = start;
            _selected = -1;
            usage ??= "";
            SetText(_usage, usage);
            _usage.Visible = usage.Length > 0;
            _help.Visible = suggestions.Count > 0;
            while (_rows.Count < count) AddRow();
            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < count;
                var row = _rows[i];
                if (row.Root.Visible != used) row.Root.Visible = used;
                if (!used) continue;
                var suggestion = suggestions[start + i];
                SetText(row.Name, suggestion.Text);
                SetText(row.Hint, suggestion.Hint);
                row.Hint.Visible = !string.IsNullOrEmpty(suggestion.Hint);
            }
        }

        if (selected != _selected)
        {
            _selected = selected;
            for (int i = 0; i < count; i++)
            {
                bool isSelected = start + i == selected;
                var row = _rows[i];
                row.Marker.Modulate = isSelected ? Color4.White : new Color4(1f, 1f, 1f, 0f);
                if (isSelected) row.Name.AddThemeColorOverride("font_color", GameCanvasThemes.Gold);
                else row.Name.RemoveThemeColorOverride("font_color");
            }
        }

        bool show = _typing && (_usage.Visible || count > 0);
        if (_suggestionPanel.Visible != show) _suggestionPanel.Visible = show;
    }

    private void SetTyping(bool typing)
    {
        _typing = typing;
        _box.ThemeTypeVariation = typing ? OpenPanel : ClosedPanel;
        _input.Visible = typing;
        _scroll.MouseFilter = typing ? MouseFilter.Pass : MouseFilter.Ignore;
        _scroll.StickToBottom = true;
        if (typing) return;

        _suggestionPanel.Visible = false;
        if (_input.HasFocus) _input.ReleaseFocus();
        _syncingInput = true;
        _input.Text = "";
        _syncingInput = false;
        _inputRevision = -1;
        _suggestionVersion = -1;
    }

    private void SyncLines(ChatLog log)
    {
        _log = log;
        _logVersion = log.Version;
        var messages = log.Messages;
        while (_labels.Count < messages.Count)
        {
            var label = new Label { Autowrap = true, ShadowColor = ShadowColor, MouseFilter = MouseFilter.Ignore };
            _labels.Add(label);
            _lines.AddChild(label);
        }

        while (_labels.Count > messages.Count)
        {
            _lines.RemoveChild(_labels[^1]);
            _labels.RemoveAt(_labels.Count - 1);
        }

        for (int i = 0; i < messages.Count; i++)
        {
            SetText(_labels[i], messages[i].Text);
            _labels[i].FontColor = messages[i].Color;
        }
    }

    private void PlaceBox()
    {
        float left = _onLeft ? 0f : 1f - WidthShare;
        float right = _onLeft ? WidthShare : 1f;
        float inner = _onLeft ? EdgeMargin : 0f;
        float outer = _onLeft ? 0f : -EdgeMargin;
        _box.SetAnchors(left, 0f, right, HeightShare);
        _box.SetOffsets(inner, EdgeMargin, outer, 0f);
        _suggestionPanel.SetAnchors(left, HeightShare, right, HeightShare);
        _suggestionPanel.SetOffsets(inner, SuggestionGap, outer, SuggestionGap);
        _suggestionPanel.GrowVertical = GrowDirection.End;
    }

    private void AddRow()
    {
        var root = new HBoxContainer { MouseFilter = MouseFilter.Ignore };
        root.AddThemeConstantOverride("separation", 6f);
        var marker = root.AddChild(new Label { Text = ">", MouseFilter = MouseFilter.Ignore, Modulate = new Color4(1f, 1f, 1f, 0f) });
        var name = root.AddChild(new Label { MouseFilter = MouseFilter.Ignore });
        var hint = root.AddChild(new Label { ThemeTypeVariation = "MutedLabel", Overrun = TextOverrun.Ellipsis, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilter.Ignore });
        _suggestionList.InsertChild(_suggestionList.Children.Count - 1, root);
        _rows.Add(new SuggestionRow(root, marker, name, hint));
    }

    private readonly record struct SuggestionRow(HBoxContainer Root, Label Marker, Label Name, Label Hint);

    private sealed class ChatScroll : ScrollContainer
    {
        public bool StickToBottom { get; set; } = true;

        protected override bool OnScroll(Vector2 localPosition, float delta)
        {
            bool handled = base.OnScroll(localPosition, delta);
            StickToBottom = ScrollOffset >= MaxScroll - 1f;
            return handled;
        }
    }
}
