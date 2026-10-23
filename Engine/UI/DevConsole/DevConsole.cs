using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevConsole : IDisposable
{
    private const float WidthRatio = 0.4f;

    private static readonly (string Label, Debug.MessageType Type, Color4 Color)[] Filters =
    {
        ("All", Debug.MessageType.Info, DarkTheme.Text),
        ("Success", Debug.MessageType.Success, DarkTheme.Success),
        ("Warning", Debug.MessageType.Warning, DarkTheme.Warning),
        ("Error", Debug.MessageType.Error, DarkTheme.Error)
    };

    private readonly CanvasLayer _layer = new CanvasLayer("Console");
    private readonly ItemList _lines = new ItemList { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly TextEdit _selectable = new TextEdit { Editable = false, Visible = false, SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly ConsoleInput _input = new ConsoleInput { PlaceholderText = "Command, Tab completes, Up/Down for history", SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Button[] _filterButtons = new Button[Filters.Length];
    private readonly long[] _shownCounts = new long[Filters.Length];
    private readonly List<Debug.ConsoleLine> _buffer = new List<Debug.ConsoleLine>();
    private Button _select;
    private SkiaCanvasFont _font;
    private Debug.MessageType _filter = Debug.MessageType.Info;
    private int _shownVersion = -1;
    private Debug.MessageType _shownFilter;
    private bool _wasVisible;

    public static DevConsole Instance { get; private set; }

    private bool _drawThisFrame;

    public static void Update()
    {
        if (Application.IsHeadless) return;
        if (!Debug.IsVisible)
        {
            if (Instance != null)
            {
                Instance._wasVisible = false;
                Instance._drawThisFrame = false;
            }
            return;
        }

        Instance ??= new DevConsole();
        Instance.Tick(Time.Delta);
    }

    public static void Draw()
    {
        if (Instance == null || !Instance._drawThisFrame) return;
        Instance._layer.Render();
    }

    public static void Shutdown()
    {
        Instance?.Dispose();
        Instance = null;
    }

    private DevConsole()
    {
        _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, DarkTheme.FontPath), (int)DarkTheme.FontSize);
        var root = _layer.Root;
        root.Theme = DarkTheme.Create(_font);
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = new Vector2(1280f, 720f);
        root.Stretch.ScaleStep = 0.5f;
        root.Stretch.MinScale = 1f;

        var panel = root.Add(new PanelContainer { Name = "Console" }, UiLayer.Debug);
        panel.AnchorLeft = 0f;
        panel.AnchorRight = WidthRatio;
        panel.AnchorTop = 0f;
        panel.AnchorBottom = 1f;

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6f);
        panel.AddChild(column);
        column.AddChild(BuildHeader());
        column.AddChild(_lines);
        column.AddChild(_selectable);
        column.AddChild(BuildInputRow());

        _lines.ItemRightClicked += index => _layer.Root.Clipboard?.SetText(_lines.Items[index].Text);
        _input.TextSubmitted += Submit;
        _input.HistoryRequested += direction => _input.SetTextAtEnd(Debug.StepHistory(_input.Text, direction));
        _input.CompletionRequested += () => _input.SetTextAtEnd(Debug.Complete(_input.Text));
    }

    private HBoxContainer BuildHeader()
    {
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "CONSOLE", FontColor = DarkTheme.Accent });
        for (int i = 0; i < Filters.Length; i++)
        {
            int index = i;
            var button = new Button { ThemeTypeVariation = "SmallButton", ToggleMode = true, TooltipText = "Show only these messages" };
            button.AddThemeColorOverride("font_color", Filters[i].Color);
            button.Pressed += _ => SetFilter(Filters[index].Type);
            _filterButtons[i] = button;
            header.AddChild(button);
        }

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore });
        var copy = new Button { Text = "Copy all", ThemeTypeVariation = "SmallButton", TooltipText = "Copy every message in the buffer" };
        copy.Pressed += _ => _layer.Root.Clipboard?.SetText(Debug.ConsoleTextFor(_filter));
        _select = new Button { Text = "Select", ThemeTypeVariation = "SmallButton", ToggleMode = true, TooltipText = "Show the messages as text to select and copy with Ctrl+C. Right-click a line to copy it" };
        _select.Toggled += (_, on) => SetSelectMode(on);
        header.AddChild(copy);
        header.AddChild(_select);
        return header;
    }

    private HBoxContainer BuildInputRow()
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = ">", FontColor = DarkTheme.Accent });
        row.AddChild(_input);
        var send = new Button { Text = "Send" };
        send.Pressed += _ => Submit(_input.Text);
        row.AddChild(send);
        return row;
    }

    private void Tick(float deltaTime)
    {
        if (!_wasVisible || Debug.TakeFocusRequest()) _input.GrabFocus();
        _wasVisible = true;

        RefreshFilters();
        RefreshLines();
        _layer.Update(deltaTime);
        _drawThisFrame = true;
    }

    private void RefreshFilters()
    {
        for (int i = 0; i < Filters.Length; i++)
        {
            long count = Debug.CountOf(Filters[i].Type);
            var button = _filterButtons[i];
            button.ButtonPressed = Filters[i].Type == _filter;
            button.Visible = i == 0 || count > 0;
            if (count == _shownCounts[i] && button.Text.Length > 0) continue;
            _shownCounts[i] = count;
            button.Text = $"{Filters[i].Label} {count}";
        }
    }

    private void RefreshLines()
    {
        int version = Debug.MessageVersion;
        if (version == _shownVersion && _filter == _shownFilter) return;

        bool atBottom = _lines.MaxScroll <= 0f || _lines.Items.Count == 0 || _shownVersion < 0 || IsScrolledToBottom();
        _shownVersion = version;
        _shownFilter = _filter;
        Debug.CopyLines(_buffer, _filter, Debug.MaxShownLines);

        _lines.Clear();
        foreach (var line in _buffer)
        {
            var item = _lines.AddItem(line.Text);
            item.TextColor = new Color4(line.Color.X, line.Color.Y, line.Color.Z, line.Color.W);
            item.Tooltip = line.Text.Length > 60 ? line.Text : "";
        }
        if (atBottom && _lines.Items.Count > 0) _lines.EnsureVisible(_lines.Items.Count - 1);
        if (_selectable.Visible) _selectable.Text = Debug.ConsoleTextFor(_filter);
    }

    private bool IsScrolledToBottom()
    {
        return _lines.IsScrolledToBottom;
    }

    private void SetFilter(Debug.MessageType type)
    {
        _filter = type;
        _shownVersion = -1;
    }

    private void SetSelectMode(bool on)
    {
        _selectable.Visible = on;
        _lines.Visible = !on;
        if (on) _selectable.Text = Debug.ConsoleTextFor(_filter);
    }

    private void Submit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Debug.Submit(text);
        _input.SetTextAtEnd("");
        _input.GrabFocus();
    }

    public void Dispose()
    {
        _layer.Dispose();
        _font?.Dispose();
        _font = null;
    }
}
