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
    private readonly Dictionary<SelectableRow, Button> _deleteButtons = new Dictionary<SelectableRow, Button>();
    private readonly ConfirmDialog _confirm = new ConfirmDialog();
    private ModConfig _selected;
    private CreateGameSetDialog _create;
    private int _shownVersion = -1;

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

        _create = new CreateGameSetDialog(_menu);
        var tools = new HBoxContainer();
        var download = MenuLayout.Button(_menu, "Download mods", null);
        download.Disabled = true;
        download.TooltipText = "Mod hub - coming soon: a place to find and download mods, texture packs and ships from other players";
        tools.AddChild(download);
        var create = MenuLayout.Button(_menu, "Create", _create.Open);
        create.TooltipText = "Create a new GameSet or an extension of an existing one";
        tools.AddChild(create);
        tools.AddChild(MenuLayout.Button(_menu, "GameSet editor", () => OpenEditor("Overview")));
        frame.Body.AddChild(tools);

        frame.Footer.AddChild(MenuLayout.Button(_menu, "Ship viewer", _menu.Open<ShipViewerWindow>));
        frame.Footer.AddChild(MenuLayout.Button(_menu, "Back", _menu.SetStateToMain));
        var packs = MenuLayout.Button(_menu, "Texture packs", _menu.Open<TexturePacksWindow>);
        packs.TooltipText = "Global texture packs: enable several and order them, the top pack wins";
        frame.Footer.AddChild(packs);
    }

    public override void Tick(float deltaTime)
    {
        if (_shownVersion != _menu.GameSetsVersion) OnShow();
    }

    public override void OnShow()
    {
        _shownVersion = _menu.GameSetsVersion;
        _rows.ClearChildren();
        _rowControls.Clear();
        _deleteButtons.Clear();
        foreach (var mod in _menu.AllGameSets)
        {
            var row = new SelectableRow(IconSize) { Metadata = mod, Icon = mod.Icon, FlipIcon = true };
            row.AddLine($"{mod.ModName}  v{mod.Version}");
            row.AddLine($"{mod.ModId} by {(string.IsNullOrEmpty(mod.Author) ? "unknown" : mod.Author)}  -  {mod.Description}", "MutedLabel");
            if (Spacebox.Game.Resource.Content.GameSetRoles.IsEmpty(mod.ModId))
                row.AddLine("Read-only base of every new GameSet - press Create to start your own", "MutedLabel");
            if (Spacebox.Game.Resource.Content.GameSetRoles.IsExtension(mod.Extends))
            {
                string includes = mod.Includes != null && mod.Includes.Count > 0 ? $", includes {string.Join(", ", mod.Includes)}" : "";
                row.AddLine($"Extension of {Spacebox.Game.Resource.Content.ExtensionTargets.Describe(mod.Extends)}{includes} - pick it when creating a world", "MutedLabel");
            }
            if (_menu.CanDeleteGameSet(mod)) _deleteButtons[row] = AddDeleteButton(row, mod);
            row.Selected += Select;
            row.Activated += r => _menu.OpenGameSetEditor((ModConfig)r.Metadata, "Overview");
            _rows.AddChild(row);
            _rowControls.Add(row);
        }

        if (_selected == null || !_menu.AllGameSets.Contains(_selected)) _selected = _menu.AllGameSets.Count > 0 ? _menu.AllGameSets[0] : null;
        foreach (var row in _rowControls) row.IsSelected = ReferenceEquals(row.Metadata, _selected);
        UpdateDeleteButtons();
    }

    private Button AddDeleteButton(SelectableRow row, ModConfig mod)
    {
        var button = new Button { Text = "X", ThemeTypeVariation = "SmallButton", SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(24f, 22f), TooltipText = "Delete this mod" };
        button.AddThemeColorOverride("font_color", DeleteColor);
        button.AddThemeColorOverride("font_hover_color", DeleteColor);
        button.Pressed += _ => AskDelete(mod);
        row.Row.AddChild(button);
        return button;
    }

    private static readonly Color4 DeleteColor = new Color4(0.85f, 0.15f, 0.1f, 1f);
    private const int MaxListed = 8;

    private void UpdateDeleteButtons()
    {
        foreach (var pair in _deleteButtons) pair.Value.Visible = pair.Key.IsSelected;
    }

    private void AskDelete(ModConfig mod)
    {
        _menu.Click1?.Play();
        var dependents = _menu.DependentsOf(mod);
        string message = $"Delete '{mod.ModName}' for good? Its folder is removed from the disk.";
        if (dependents.Any)
        {
            message += "\n\nWarning: these will no longer work:";
            if (dependents.Worlds.Count > 0) message += "\nWorlds: " + Listed(dependents.Worlds);
            if (dependents.Mods.Count > 0) message += "\nMods: " + Listed(dependents.Mods.Select(m => m.DisplayName).ToList());
        }

        _confirm.Show(Root, "Delete mod", message, new[] { "Delete", "Cancel" }, result =>
        {
            if (result != 0) return;
            string error = _menu.DeleteGameSet(mod);
            if (error != null) _menu.ShowMessage("Delete mod", error);
        });
    }

    private static string Listed(IReadOnlyList<string> names)
    {
        var shown = names.Take(MaxListed).ToList();
        string text = string.Join(", ", shown);
        return names.Count > MaxListed ? $"{text} and {names.Count - MaxListed} more" : text;
    }

    private void Select(SelectableRow row)
    {
        _selected = (ModConfig)row.Metadata;
        foreach (var other in _rowControls) other.IsSelected = ReferenceEquals(other, row);
        UpdateDeleteButtons();
    }

    private void OpenEditor(string panel)
    {
        if (_selected != null) _menu.OpenGameSetEditor(_selected, panel);
    }
}
