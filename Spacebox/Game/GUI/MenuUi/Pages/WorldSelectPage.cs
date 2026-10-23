using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class WorldSelectPage : MenuPage
{
    private const float Width = 560f;
    private const float ListHeight = 190f;

    private readonly GameMenu _menu;
    private readonly ItemList _list = new ItemList { CustomMinimumSize = new Vector2(300f, ListHeight), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly TextureRect _preview = new TextureRect { StretchMode = TextureStretchMode.KeepAspectCentered, FlipV = true, CustomMinimumSize = new Vector2(ListHeight, ListHeight), MouseFilter = MouseFilter.Ignore };
    private readonly Label _noPreview = new Label { Text = "No preview yet", ThemeTypeVariation = "MutedLabel", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly Label _mode = new Label();
    private readonly Label _author = new Label { Overrun = TextOverrun.Ellipsis };
    private readonly Label _gameSet = new Label { Overrun = TextOverrun.Ellipsis };
    private readonly Label _extensions = new Label { Autowrap = true };
    private readonly Label _version = new Label();
    private readonly ConfirmDialog _confirm = new ConfirmDialog();
    private Button _play;
    private Button _edit;
    private Button _delete;
    private WorldInfo _shownWorld;
    private int _shownCount = -1;

    public WorldSelectPage(GameMenu menu)
    {
        _menu = menu;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window("Select World", Width);
        AddChild(frame.Root);

        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 10f);
        top.AddChild(_list);
        var previewBox = new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(ListHeight, ListHeight) };
        previewBox.AddChild(_preview);
        previewBox.AddChild(_noPreview);
        top.AddChild(previewBox);
        frame.Body.AddChild(top);

        var info = new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(0f, 96f) };
        var lines = new VBoxContainer();
        lines.AddThemeConstantOverride("separation", 2f);
        foreach (var label in new Control[] { _mode, _author, _gameSet, _extensions, _version }) lines.AddChild(label);
        info.AddChild(lines);
        frame.Body.AddChild(info);

        var worldButtons = new HBoxContainer();
        _play = MenuLayout.Button(_menu, "Play", () => _menu.OpenWorld(_menu.selectedWorld));
        _edit = MenuLayout.Button(_menu, "Edit", Edit);
        _delete = MenuLayout.Button(_menu, "Delete", AskDelete, false);
        _delete.CustomMinimumSize = new Vector2(90f, MenuLayout.ButtonHeight);
        _delete.TooltipText = "Delete the world";
        worldButtons.AddChild(_play);
        worldButtons.AddChild(_edit);
        worldButtons.AddChild(_delete);
        frame.Body.AddChild(worldButtons);

        frame.Footer.AddChild(MenuLayout.Button(_menu, "Create New World", CreateNew));
        frame.Footer.AddChild(MenuLayout.Button(_menu, "Back", _menu.SetStateToMain));

        _list.ItemSelected += index =>
        {
            _menu.Click1?.Play();
            if (_list.Items[index].Metadata is WorldInfo world) _menu.selectedWorld = world;
        };
        _list.ItemActivated += index =>
        {
            if (_list.Items[index].Metadata is WorldInfo world) _menu.OpenWorld(world);
        };
    }

    public override void OnShow()
    {
        _shownCount = -1;
        _shownWorld = null;
    }

    public override void Tick(float deltaTime)
    {
        if (_shownCount != _menu.Worlds.Count) RebuildList();
        if (!ReferenceEquals(_shownWorld, _menu.selectedWorld)) ShowWorld(_menu.selectedWorld);
    }

    private void RebuildList()
    {
        _shownCount = _menu.Worlds.Count;
        _list.Clear();
        int selected = -1;
        for (int i = 0; i < _menu.Worlds.Count; i++)
        {
            var world = _menu.Worlds[i];
            var item = _list.AddItem(world.Name, null, world);
            if (ReferenceEquals(world, _menu.selectedWorld)) selected = i;
        }
        if (selected >= 0) _list.Select(selected);
        _shownWorld = null;
    }

    private void ShowWorld(WorldInfo world)
    {
        _shownWorld = world;
        bool has = world != null;
        _play.Disabled = !has;
        _edit.Disabled = !has;
        _delete.Disabled = !has;
        _preview.Texture = world?.WorldPreview;
        _preview.Visible = world?.WorldPreview != null;
        _noPreview.Visible = has && world.WorldPreview == null;

        if (!has)
        {
            _mode.Text = "Select a world";
            _author.Text = _gameSet.Text = _extensions.Text = _version.Text = "";
            return;
        }

        int index = _menu.Worlds.IndexOf(world);
        if (index >= 0 && _list.SelectedIndex != index) _list.Select(index);

        var mod = _menu.GetModById(world.ModId);
        _mode.Text = "Game mode: " + Spacebox.Game.Player.GameModes.GameModeCatalog.NameOf(world.GameMode);
        _author.Text = "Author: " + (string.IsNullOrWhiteSpace(world.Author) ? "-" : world.Author);
        _gameSet.Text = $"GameSet: {world.ModId} ({mod?.Version ?? "unknown version"})";
        var extensions = WorldGameSetCheck.ExtensionsOf(world);
        _extensions.Text = extensions.Length > 0 ? "Extensions: " + string.Join(", ", extensions) : "";
        _extensions.Visible = extensions.Length > 0;
        _version.Text = "Version: " + world.GameVersion;
        _version.ThemeTypeVariation = world.GameVersion != Application.Version ? "WarningLabel" : "Label";
    }

    private void Edit()
    {
        var world = _menu.selectedWorld;
        if (world == null) return;
        _menu.newWorldName = world.Name;
        _menu.newWorldAuthor = world.Author;
        _menu.newWorldSeed = world.Seed;
        _menu.SelectedGameModeIndex = (int)world.GameMode;
        _menu.IsEditMode = true;
        _menu.SetStateToNewWorld();
    }

    private void CreateNew()
    {
        _menu.GenerateRandomSeedAndName();
        _menu.IsEditMode = false;
        _menu.SetStateToNewWorld();
    }

    private void AskDelete()
    {
        var world = _menu.selectedWorld;
        if (world == null || Root == null) return;
        _confirm.Show(Root, "Delete world", $"Delete \"{world.Name}\" for ever?", new[] { "Delete", "Cancel" }, result =>
        {
            if (result != 0) return;
            _menu.DeleteWorld(world);
            _menu.selectedWorld = _menu.Worlds.Count > 0 ? _menu.Worlds[0] : null;
        });
    }
}
