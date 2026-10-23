using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ModsPage : MenuPage
{
    private const float Width = 620f;
    private const float IconSize = 40f;

    private readonly GameMenu _menu;
    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly List<SelectableRow> _rowControls = new List<SelectableRow>();
    private ModConfig _selected;

    public ModsPage(GameMenu menu)
    {
        _menu = menu;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window("GameSets", Width);
        AddChild(frame.Root);
        _rows.AddThemeConstantOverride("separation", 0f);

        frame.Body.AddChild(MenuLayout.ScrollList(_rows, 250f));

        var tools = new HBoxContainer();
        var download = MenuLayout.Button(_menu, "Download mods", null);
        download.Disabled = true;
        download.TooltipText = "Mod hub - coming soon: a place to find and download mods, texture packs and ships from other players";
        tools.AddChild(download);
        tools.AddChild(MenuLayout.Button(_menu, "GameSet editor", () => OpenEditor("Overview")));
        tools.AddChild(MenuLayout.Button(_menu, "Generation editor", () => OpenEditor("Generation")));
        frame.Body.AddChild(tools);

        frame.Footer.AddChild(MenuLayout.Button(_menu, "Ship viewer", _menu.Open<ShipViewerWindow>));
        frame.Footer.AddChild(MenuLayout.Button(_menu, "Back", _menu.SetStateToMain));
        var packs = MenuLayout.Button(_menu, "Texture packs", _menu.Open<TexturePacksWindow>);
        packs.TooltipText = "Global texture packs: enable several and order them, the top pack wins";
        frame.Footer.AddChild(packs);
    }

    public override void OnShow()
    {
        _rows.ClearChildren();
        _rowControls.Clear();
        foreach (var mod in _menu.AllGameSets)
        {
            var row = new SelectableRow(IconSize) { Metadata = mod, Icon = mod.Icon, FlipIcon = true };
            row.AddLine($"{mod.ModName}  v{mod.Version}");
            row.AddLine($"{mod.ModId} by {(string.IsNullOrEmpty(mod.Author) ? "unknown" : mod.Author)}  -  {mod.Description}", "MutedLabel");
            if (!string.IsNullOrWhiteSpace(mod.Extends))
            {
                string includes = mod.Includes != null && mod.Includes.Count > 0 ? $", includes {string.Join(", ", mod.Includes)}" : "";
                row.AddLine($"Extension of '{mod.Extends}'{includes} - pick it when creating a world", "MutedLabel");
            }
            row.Selected += Select;
            row.Activated += r => _menu.OpenGameSetEditor((ModConfig)r.Metadata, "Overview");
            _rows.AddChild(row);
            _rowControls.Add(row);
        }

        if (_selected == null || !_menu.AllGameSets.Contains(_selected)) _selected = _menu.AllGameSets.Count > 0 ? _menu.AllGameSets[0] : null;
        foreach (var row in _rowControls) row.IsSelected = ReferenceEquals(row.Metadata, _selected);
    }

    private void Select(SelectableRow row)
    {
        _selected = (ModConfig)row.Metadata;
        foreach (var other in _rowControls) other.IsSelected = ReferenceEquals(other, row);
    }

    private void OpenEditor(string panel)
    {
        if (_selected != null) _menu.OpenGameSetEditor(_selected, panel);
    }
}
