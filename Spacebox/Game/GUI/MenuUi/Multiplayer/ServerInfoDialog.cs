using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ServerInfoDialog
{
    private const float Width = 520f;
    private const float IconSize = 72f;

    private readonly GameMenu _menu;
    private readonly MultiplayerWindow _window;
    private readonly MenuModal _modal;
    private readonly MenuWindowFrame _frame;
    private readonly HBoxContainer _header = new HBoxContainer();
    private readonly TextureRect _icon = new TextureRect { StretchMode = TextureStretchMode.KeepAspectCentered, FlipV = true, CustomMinimumSize = new Vector2(IconSize, IconSize) };
    private readonly Label _name = new Label { ThemeTypeVariation = "SectionLabel" };
    private readonly Label _address = new Label { ThemeTypeVariation = "MutedLabel" };
    private readonly Label _state = new Label();
    private readonly Label _players = new Label();
    private readonly GridContainer _details = new GridContainer { Columns = 2 };
    private readonly Label _description = new Label { Autowrap = true };
    private readonly PanelContainer _descriptionBox = new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(0f, 110f) };
    private readonly LineEdit _nameEdit = new LineEdit { MaxLength = ServerAddressValidator.MaxNameLength, PlaceholderText = "Server name (optional)" };
    private readonly LineEdit _hostEdit = new LineEdit { MaxLength = ServerAddressValidator.MaxHostLength, PlaceholderText = "IP address or host name" };
    private readonly NumberEdit _portEdit = new NumberEdit { Integer = true, MinValue = 1, MaxValue = 65535, CustomMinimumSize = new Vector2(120f, 0f) };
    private readonly Label _message = new Label { Autowrap = true };
    private ServerListItem _item;
    private bool _addMode;

    public ServerInfoDialog(GameMenu menu, MultiplayerWindow window)
    {
        _menu = menu;
        _window = window;
        _frame = MenuLayout.Window("Server", Width);
        _modal = new MenuModal(_frame.Root);
        Build();
    }

    public bool IsOpen => _modal.IsOpen;

    private void Build()
    {
        _header.AddThemeConstantOverride("separation", 12f);
        var iconBox = new PanelContainer { ThemeTypeVariation = "InsetPanel", SizeFlagsVertical = SizeFlags.ShrinkBegin };
        iconBox.AddChild(_icon);
        _header.AddChild(iconBox);
        var lines = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        lines.AddThemeConstantOverride("separation", 2f);
        lines.AddChild(_name);
        lines.AddChild(_address);
        lines.AddChild(_state);
        lines.AddChild(_players);
        _header.AddChild(lines);
        _frame.Body.AddChild(_header);

        _details.AddThemeConstantOverride("h_separation", 14f);
        _frame.Body.AddChild(_details);
        var scroll = new ScrollContainer();
        scroll.AddChild(_description);
        _description.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        _descriptionBox.AddChild(scroll);
        _frame.Body.AddChild(_descriptionBox);

        _frame.Body.AddChild(MenuLayout.Field("Name", _nameEdit));
        _frame.Body.AddChild(MenuLayout.Field("Address", _hostEdit));
        var portRow = new HBoxContainer();
        portRow.AddChild(MenuLayout.Field("Port", _portEdit));
        _portEdit.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        _frame.Body.AddChild(portRow);
        _frame.Body.AddChild(_message);
        _hostEdit.TextSubmitted += _ => Save();
    }

    public void OpenAdd()
    {
        _item = null;
        _addMode = true;
        _nameEdit.Text = "";
        _hostEdit.Text = "";
        _portEdit.Value = Engine.Networking.NetworkManager.DefaultPort;
        Show();
    }

    public void OpenInfo(ServerListItem item)
    {
        if (item == null) return;
        _item = item;
        _addMode = false;
        _nameEdit.Text = item.DisplayName;
        _hostEdit.Text = item.Entry.IP;
        _portEdit.Value = item.Entry.Port;
        _window.Browser.Ping(item);
        Show();
    }

    public void Close() => _modal.Close();

    private void Show()
    {
        _frame.Title.Text = _addMode ? "Add server" : "Server info";
        _header.Visible = !_addMode;
        _details.Visible = !_addMode;
        _descriptionBox.Visible = !_addMode;
        SetMessage(_item != null && !_item.IsSaved ? "Found in your local network." : "", "MutedLabel");
        BuildButtons();
        Refresh();
        _modal.Open(MenuCanvas.Current?.Root);
        _hostEdit.GrabFocus();
    }

    private void BuildButtons()
    {
        _frame.Footer.ClearChildren();
        string saveLabel = _addMode ? "Add" : _item.IsSaved ? "Save" : "Add to list";
        _frame.Footer.AddChild(MenuLayout.Button(_menu, saveLabel, Save));
        if (!_addMode)
        {
            _frame.Footer.AddChild(MenuLayout.Button(_menu, "Join", () =>
            {
                var item = _item;
                Close();
                _window.Join(item);
            }));
        }
        _frame.Footer.AddChild(MenuLayout.Button(_menu, _addMode ? "Cancel" : "Close", Close));
    }

    public void Refresh()
    {
        if (_addMode || _item == null || !IsOpen) return;
        _icon.Texture = _window.Icons.Get(_item.IconHash);
        _name.Text = _item.Status?.Name ?? _item.DisplayName;
        _address.Text = ServerText.AddressLine(_item);
        _state.Text = _item.State == ServerOnlineState.Online ? $"Online, {_item.PingMs} ms" : ServerText.StateLabel(_item);
        _state.AddThemeColorOverride("font_color", ServerTextColors.State(_item));
        _players.Text = ServerText.PlayersLabel(_item);

        _details.ClearChildren();
        var status = _item.Status;
        if (status != null)
        {
            AddDetail("Version:", ServerText.VersionLabel(_item), !_item.IsCompatible);
            AddDetail("GameSet:", GameSetLabel(status.ModId), false);
            AddDetail("Game mode:", ServerText.GameModeLabel(status.GameMode), false);
            AddDetail("World:", status.WorldName, false);
            AddDetail("Type:", status.IsDedicated ? "Dedicated server" : "Hosted by a player", false);
        }

        string description = _item.Description;
        bool empty = string.IsNullOrWhiteSpace(description);
        _description.Text = empty ? status == null ? "No information - the server did not answer." : "No description." : description;
        _description.ThemeTypeVariation = empty ? "MutedLabel" : "Label";
    }

    private void AddDetail(string label, string value, bool error)
    {
        _details.AddChild(new Label { Text = label, ThemeTypeVariation = "MutedLabel" });
        var text = new Label { Text = value ?? "" };
        if (error) text.ThemeTypeVariation = "ErrorLabel";
        _details.AddChild(text);
    }

    private string GameSetLabel(string modId)
    {
        var mod = _menu.GetModById(modId);
        return mod != null ? $"{mod.ModName} ({modId})" : $"{modId} (downloaded on join)";
    }

    private void SplitPort()
    {
        if (_hostEdit.Text.IndexOf(':') < 0) return;
        ServerAddressValidator.Resolve(_hostEdit.Text, (int)_portEdit.Value, out var host, out var port);
        if (host == _hostEdit.Text) return;
        _hostEdit.Text = host;
        _portEdit.Value = port;
    }

    private void Save()
    {
        SplitPort();
        string host = _hostEdit.Text;
        int port = (int)_portEdit.Value;
        if (!ServerAddressValidator.TryValidate(host, port, out var error))
        {
            SetMessage(error, "ErrorLabel");
            return;
        }

        var item = _item;
        Close();
        _window.SaveServer(item, ServerAddressValidator.ResolveName(_nameEdit.Text, host), host.Trim(), port);
    }

    private void SetMessage(string text, string variation)
    {
        _message.Text = text ?? "";
        _message.ThemeTypeVariation = variation;
        _message.Visible = !string.IsNullOrEmpty(text);
    }
}
