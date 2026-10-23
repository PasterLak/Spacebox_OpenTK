using Engine.CanvasUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ServerRowView
{
    private const float IconSize = 44f;

    private readonly ServerIconCache _icons;
    private readonly Label _name;
    private readonly Label _description;
    private readonly Label _address;
    private readonly Label _state = new Label { HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = MouseFilter.Ignore };
    private readonly Label _players = new Label { HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = MouseFilter.Ignore };
    private readonly Label _version = new Label { HorizontalAlignment = HorizontalAlignment.Right, ThemeTypeVariation = "MutedLabel", MouseFilter = MouseFilter.Ignore };
    private readonly SignalBarsControl _bars = new SignalBarsControl();

    public ServerRowView(ServerListItem item, ServerIconCache icons)
    {
        Item = item;
        _icons = icons;
        Control = new SelectableRow(IconSize) { Metadata = item, FlipIcon = true };
        _name = Control.AddLine("");
        _description = Control.AddLine("", "MutedLabel");
        _address = Control.AddLine("", "MutedLabel");

        var status = new VBoxContainer { MouseFilter = MouseFilter.Ignore, CustomMinimumSize = new OpenTK.Mathematics.Vector2(150f, 0f) };
        status.AddThemeConstantOverride("separation", 1f);
        var stateRow = new HBoxContainer { Alignment = BoxAlignment.End, MouseFilter = MouseFilter.Ignore };
        stateRow.AddThemeConstantOverride("separation", 6f);
        stateRow.AddChild(_state);
        stateRow.AddChild(_bars);
        status.AddChild(stateRow);
        status.AddChild(_players);
        status.AddChild(_version);
        Control.Row.AddChild(status);
        _bars.Item = item;
        Refresh();
    }

    public ServerListItem Item { get; }
    public SelectableRow Control { get; }

    public void Refresh()
    {
        Control.Icon = _icons.Get(Item.IconHash);
        _name.Text = Item.DisplayName;
        _description.Text = ServerText.FirstLine(Item.Description);
        _address.Text = ServerText.AddressLine(Item);
        _state.Text = ServerText.StateLabel(Item);
        _state.AddThemeColorOverride("font_color", ServerTextColors.State(Item));
        _players.Text = ServerText.PlayersLabel(Item);
        _version.Text = ServerText.VersionLabel(Item);
        if (Item.IsCompatible) _version.RemoveThemeColorOverride("font_color");
        else _version.AddThemeColorOverride("font_color", ServerTextColors.Error);
    }
}
