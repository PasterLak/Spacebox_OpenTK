using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class MultiplayerPage : MenuPage
{
    private const float Width = 640f;

    private readonly MultiplayerWindow _window;
    private readonly LineEdit _nickname = new LineEdit { MaxLength = 32, PlaceholderText = "Player" };
    private readonly ChoiceBox _color = new ChoiceBox { CustomMinimumSize = new Vector2(150f, 0f) };
    private readonly ColorSwatch _swatch = new ColorSwatch(18f);
    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly List<ServerRowView> _views = new List<ServerRowView>();
    private readonly List<string> _keys = new List<string>();
    private readonly List<string> _currentKeys = new List<string>();
    private readonly PopupMenu _rowMenu = new PopupMenu();
    private ServerInfoDialog _dialog;
    private Button _join;
    private Button _info;
    private Button _remove;
    private bool _updating;
    private float _dialogRefresh;

    public MultiplayerPage(MultiplayerWindow window)
    {
        _window = window;
    }

    private GameMenu Menu => _window.menu;

    protected override void Build()
    {
        _dialog = new ServerInfoDialog(Menu, _window);
        var frame = MenuLayout.Window("Multiplayer", Width);
        AddChild(frame.Root);
        _rows.AddThemeConstantOverride("separation", 0f);

        var profile = new HBoxContainer();
        profile.AddThemeConstantOverride("separation", 8f);
        profile.AddChild(new Label { Text = "Nickname", SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _nickname.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        _nickname.TextChanged += text =>
        {
            if (!_updating) _window.SetNickname(text);
        };
        profile.AddChild(_nickname);
        profile.AddChild(new Label { Text = "Color", SizeFlagsVertical = SizeFlags.ShrinkCenter });
        profile.AddChild(_swatch);
        _color.SetChoices(_window.Colors);
        _color.IndexChanged += index =>
        {
            if (_updating) return;
            _window.SelectColor(index);
            UpdateSwatch();
        };
        profile.AddChild(_color);
        frame.Body.AddChild(profile);

        frame.Body.AddChild(MenuLayout.ScrollList(_rows, 300f));

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8f);
        _join = MenuLayout.Button(Menu, "Join", _window.JoinOrCancel);
        _info = MenuLayout.Button(Menu, "Info", () => _dialog.OpenInfo(_window.Selected));
        _remove = MenuLayout.Button(Menu, "Remove", () => _window.RemoveServer(_window.Selected));
        actions.AddChild(_join);
        actions.AddChild(_info);
        actions.AddChild(MenuLayout.Button(Menu, "Add server", _dialog.OpenAdd));
        actions.AddChild(_remove);
        frame.Body.AddChild(actions);

        frame.Footer.AddChild(MenuLayout.Button(Menu, "Refresh", () => _window.Browser.Refresh()));
        frame.Footer.AddChild(MenuLayout.Button(Menu, "Back", () =>
        {
            _dialog.Close();
            _window.Back();
        }));
    }

    public override void OnShow()
    {
        _updating = true;
        _nickname.Text = _window.Nickname;
        _color.SelectedIndex = _window.SelectedColorIndex;
        _updating = false;
        UpdateSwatch();
        _keys.Clear();
        RebuildIfChanged();
    }

    public override void Tick(float deltaTime)
    {
        _window.UpdateBrowser(deltaTime);
        RebuildIfChanged();
        foreach (var view in _views)
        {
            view.Refresh();
            view.Control.IsSelected = view.Item == _window.Selected;
        }
        UpdateButtons();
        _dialogRefresh -= deltaTime;
        if (_dialogRefresh > 0f) return;
        _dialogRefresh = 0.5f;
        _dialog.Refresh();
    }

    private void UpdateSwatch()
    {
        var colors = _window.Colors;
        int index = Math.Clamp(_window.SelectedColorIndex, 0, colors.Count - 1);
        _swatch.Color = ServerTextColors.From(MultiplayerWindow.ColorOf(colors[index]));
    }

    private void UpdateButtons()
    {
        var selected = _window.Selected;
        bool listed = _window.CanEdit(selected);
        bool connecting = ClientConnector.IsConnecting;
        _join.Text = connecting ? "Cancel" : "Join";
        _join.Disabled = !listed && !connecting;
        _info.Disabled = !listed;
        _remove.Disabled = !listed || !selected.IsSaved;
    }

    private void RebuildIfChanged()
    {
        var browser = _window.Browser;
        _currentKeys.Clear();
        foreach (var item in browser.Saved) _currentKeys.Add("s:" + item.Key);
        foreach (var item in browser.Local)
            if (!browser.IsHiddenLocal(item)) _currentKeys.Add("l:" + item.Key);
        if (browser.Local.Count == 0) _currentKeys.Add("searching");
        if (SameKeys()) return;

        _keys.Clear();
        _keys.AddRange(_currentKeys);
        _rows.ClearChildren();
        _views.Clear();

        AddSection($"Saved servers ({browser.Saved.Count})");
        if (browser.Saved.Count == 0) AddHint("No saved servers yet. Press \"Add server\" to add one.");
        foreach (var item in browser.Saved) AddRow(item);

        AddSection("Local network");
        int shown = 0;
        foreach (var item in browser.Local)
        {
            if (browser.IsHiddenLocal(item)) continue;
            AddRow(item);
            shown++;
        }
        if (shown == 0) AddHint(browser.Local.Count == 0 ? "Searching for servers in your network..." : "No other servers found in your network.");
    }

    private bool SameKeys()
    {
        if (_keys.Count != _currentKeys.Count) return false;
        for (int i = 0; i < _keys.Count; i++)
            if (_keys[i] != _currentKeys[i]) return false;
        return true;
    }

    private void AddSection(string title)
    {
        _rows.AddChild(new Label { Text = title, ThemeTypeVariation = "SectionLabel" });
    }

    private void AddHint(string text) => _rows.AddChild(new Label { Text = "   " + text, ThemeTypeVariation = "MutedLabel" });

    private void AddRow(ServerListItem item)
    {
        var view = new ServerRowView(item, _window.Icons);
        view.Control.Selected += _ => _window.Selected = item;
        view.Control.Activated += _ => _window.Join(item);
        view.Control.RightClicked += (_, position) =>
        {
            _rowMenu.ClearItems();
            _rowMenu.AddItem("Copy address", () => Root?.Clipboard?.SetText(item.Entry.Address));
            _rowMenu.Open(Root, position);
        };
        _rows.AddChild(view.Control);
        _views.Add(view);
    }
}
