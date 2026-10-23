using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Preview;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Resource;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class ShipViewerPage : MenuPage
{
    private const float Width = 1100f;
    private const float Height = 540f;
    private const float ListWidth = 320f;

    private readonly GameMenu _menu;
    private readonly ShipViewerSession _session;
    private readonly ItemList _worlds = new ItemList { CustomMinimumSize = new Vector2(0f, 190f) };
    private readonly ItemList _ships = new ItemList { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly HSlider _light = new HSlider { MinValue = 0, MaxValue = 1.5, Step = 0.01, CustomMinimumSize = new Vector2(110f, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private readonly CheckBox _power = new CheckBox("Power", false) { TooltipText = "Turns all energy blocks on or off (look only)" };
    private readonly CheckBox _engines = new CheckBox("Engines", false);
    private readonly CheckBox _autoFire = new CheckBox("Autofire", false);
    private readonly ChoiceBox _projectile = new ChoiceBox { CustomMinimumSize = new Vector2(190f, 0f), TooltipText = "Pick a projectile, then click on the ship to shoot it" };
    private readonly ColorSwatch _projectileColor = new ColorSwatch(16f);
    private readonly Label _shipInfo = new Label { Overrun = TextOverrun.Ellipsis };
    private readonly Label _contentInfo = new Label();
    private readonly Label _status = new Label { ThemeTypeVariation = "WarningLabel", Overrun = TextOverrun.Ellipsis };
    private readonly Label _notice = new Label { ThemeTypeVariation = "WarningLabel", Overrun = TextOverrun.Ellipsis };
    private readonly List<string> _projectileNames = new List<string>();
    private ShipExportDialog _export;
    private Button _fire;
    private Button _picture;
    private Button _exportButton;
    private Button _restore;
    private int _worldCount = -1;
    private bool _scanning;
    private ShipScanResult _shownShips;
    private bool _updating;

    public ShipViewerPage(GameMenu menu, ShipViewerSession session)
    {
        _menu = menu;
        _session = session;
    }

    public override bool ShowsLogo => false;

    protected override void Build()
    {
        _export = new ShipExportDialog(_menu, _session);
        var frame = MenuLayout.Window("Ship viewer", Width, Height);
        AddChild(frame.Root);

        var split = new HBoxContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
        split.AddThemeConstantOverride("separation", 12f);
        frame.Body.AddChild(split);

        var lists = new VBoxContainer { CustomMinimumSize = new Vector2(ListWidth, 0f) };
        lists.AddChild(new Label { Text = "Worlds", ThemeTypeVariation = "SectionLabel" });
        lists.AddChild(_worlds);
        lists.AddChild(new Label { Text = "Ships", ThemeTypeVariation = "SectionLabel" });
        lists.AddChild(_ships);
        split.AddChild(lists);
        _worlds.ItemSelected += index =>
        {
            if (!_updating) _session.SelectWorld(_worlds.Items[index].Metadata as SavedWorld);
        };
        _ships.ItemSelected += index =>
        {
            if (!_updating && _ships.Items[index].Metadata is SavedShip ship) _session.SelectShip(ship);
        };

        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        right.AddThemeConstantOverride("separation", 6f);
        right.AddChild(new PreviewView(_session.Viewport) { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand });
        right.AddChild(BuildViewControls());
        right.AddChild(BuildShipControls());
        right.AddChild(_shipInfo);
        right.AddChild(_contentInfo);
        right.AddChild(_status);
        right.AddChild(_notice);
        split.AddChild(right);

        var back = MenuLayout.Button(_menu, "Back", _menu.Open<ModsWindow>, false);
        back.CustomMinimumSize = new Vector2(220f, MenuLayout.ButtonHeight);
        frame.Footer.AddChild(MenuLayout.Spacer());
        frame.Footer.AddChild(back);
        frame.Footer.AddChild(MenuLayout.Spacer());
    }

    private HBoxContainer BuildViewControls()
    {
        var row = Row();
        row.AddChild(new Label { Text = "Light", SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _light.ValueChanged += value => _session.AmbientLight = (float)value;
        row.AddChild(_light);
        _power.Toggled += (_, on) => _session.Powered = on;
        row.AddChild(_power);
        _engines.Toggled += (_, on) => _session.EnginesOn = on;
        row.AddChild(_engines);
        _fire = SmallButton("Fire", null, _session.FireWeapons);
        row.AddChild(_fire);
        _autoFire.Toggled += (_, on) => _session.AutoFire = on;
        row.AddChild(_autoFire);
        return row;
    }

    private HBoxContainer BuildShipControls()
    {
        var row = Row();
        row.AddChild(new Label { Text = "Shoot", SizeFlagsVertical = SizeFlags.ShrinkCenter });
        row.AddChild(_projectileColor);
        _projectile.IndexChanged += index =>
        {
            if (!_updating) _session.ProjectileName = index <= 0 ? null : _projectileNames[index];
        };
        row.AddChild(_projectile);
        _picture = SmallButton("Save picture", "Renders the ship and saves it to the screenshots folder", _session.SavePicture);
        _exportButton = SmallButton("Export", "Saves the ship as one file in the Ships folder to share it; place it in a creative world from the Ships window", _export.Open);
        _restore = SmallButton("Restore ship", "Reloads the ship from its save and removes all damage", _session.RestoreShip);
        row.AddChild(_picture);
        row.AddChild(_exportButton);
        row.AddChild(_restore);
        return row;
    }

    private static HBoxContainer Row()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12f);
        return row;
    }

    private Button SmallButton(string text, string tooltip, Action click)
    {
        var button = new Button { Text = text, TooltipText = tooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0f, 24f) };
        button.Pressed += _ =>
        {
            _menu.Click1?.Play();
            click();
        };
        return button;
    }

    public override void OnShow()
    {
        _worldCount = -1;
        _shownShips = null;
        _projectileNames.Clear();
        _projectileNames.Add("None");
        foreach (var projectile in GameAssets.Projectiles.Values) _projectileNames.Add(projectile.Name);
        _projectile.SetChoices(_projectileNames);
        Tick(0f);
    }

    public override void Tick(float deltaTime)
    {
        _updating = true;
        SyncWorlds();
        SyncShips();
        SyncControls();
        _updating = false;
        SyncInfo();
    }

    private void SyncWorlds()
    {
        var worlds = _session.Worlds;
        if (worlds.Count != _worldCount || _session.IsScanning != _scanning)
        {
            _worldCount = worlds.Count;
            _scanning = _session.IsScanning;
            _worlds.Clear();
            if (_scanning) _worlds.AddHeader("Searching worlds for ships...");
            else if (worlds.Count == 0) _worlds.AddHeader("No worlds with ships");
            foreach (var world in worlds)
            {
                var item = _worlds.AddItem(world.Name, null, world);
                item.Secondary = $"v{world.GameVersion}  {world.ModId}";
                item.Tooltip = world.Message ?? "";
                item.Disabled = !world.CanBeViewed;
            }
        }

        int selected = IndexOf(_worlds, _session.SelectedWorld);
        if (_worlds.SelectedIndex != selected) _worlds.Select(selected);
    }

    private void SyncShips()
    {
        var result = _session.Ships;
        if (!ReferenceEquals(result, _shownShips) || (_session.SelectedWorld == null && _ships.Items.Count == 0))
        {
            _shownShips = result;
            _ships.Clear();
            if (_session.SelectedWorld == null) _ships.AddHeader("Select a world");
            else if (result != null)
            {
                for (int i = 0; i < result.Ships.Count; i++)
                {
                    var ship = result.Ships[i];
                    var item = _ships.AddItem(result.Label(i), null, ship);
                    item.Secondary = $"{ship.Stats.Blocks} blocks";
                    item.Tooltip = $"Entity {ship.EntityId}";
                }
                if (result.UnreadableFiles > 0) _ships.AddHeader($"{result.UnreadableFiles} damaged entity files skipped");
            }
        }

        int selected = IndexOf(_ships, _session.SelectedShip);
        if (_ships.SelectedIndex != selected) _ships.Select(selected);
    }

    private static int IndexOf(ItemList list, object metadata)
    {
        if (metadata == null) return -1;
        var items = list.Items;
        for (int i = 0; i < items.Count; i++)
            if (ReferenceEquals(items[i].Metadata, metadata)) return i;
        return -1;
    }

    private void SyncControls()
    {
        var content = _session.Content;
        bool ready = content != null && content.IsReady;
        if (!_light.IsMouseDown) _light.Value = _session.AmbientLight;
        _power.ButtonPressed = _session.Powered;
        _engines.ButtonPressed = _session.EnginesOn;
        _engines.Disabled = !ready || !content.HasEngines;
        _engines.TooltipText = _engines.Disabled ? "The ship has no engines" : "";
        _autoFire.ButtonPressed = _session.AutoFire;
        bool cannons = ready && content.HasCannons;
        _fire.Disabled = !cannons;
        _autoFire.Disabled = !cannons;
        _picture.Disabled = !ready;
        _exportButton.Disabled = !ready || _session.IsExporting;
        _restore.Disabled = content == null || !content.CanRestore;

        string name = _session.ProjectileName;
        bool found = name != null && GameAssets.TryGetProjectileByName(name, out var projectile);
        int index = found ? Math.Max(0, _projectileNames.IndexOf(name)) : 0;
        if (_projectile.SelectedIndex != index) _projectile.SelectedIndex = index;
        _projectileColor.Visible = found;
        if (found && GameAssets.TryGetProjectileByName(name, out projectile)) _projectileColor.Color = new Color4(projectile.Color.R, projectile.Color.G, projectile.Color.B, 1f);
    }

    private void SyncInfo()
    {
        var ship = _session.SelectedShip;
        if (ship == null)
        {
            _shipInfo.Text = "Select a ship to view it";
            _shipInfo.ThemeTypeVariation = "MutedLabel";
        }
        else
        {
            var size = ship.Stats.Size;
            _shipInfo.Text = $"Entity {ship.EntityId}  -  {ship.Stats.Blocks} blocks, {ship.Stats.Chunks} chunks, {size.X} x {size.Y} x {size.Z}";
            _shipInfo.ThemeTypeVariation = "Label";
        }

        SetContentInfo(ship == null ? null : _session.Content);
        _status.Text = _session.Status ?? "";
        _status.Visible = _status.Text.Length > 0;
        string notice = _session.GameSetNotice;
        if (_session.SelectedWorld?.Status == SavedWorldStatus.NeedsUpgrade) notice = string.IsNullOrEmpty(notice) ? _session.SelectedWorld.Message : notice + "  " + _session.SelectedWorld.Message;
        _notice.Text = notice ?? "";
        _notice.Visible = _notice.Text.Length > 0;
    }

    private void SetContentInfo(ShipPreviewContent content)
    {
        string text = "";
        string variation = "MutedLabel";
        if (content != null)
        {
            if (!string.IsNullOrEmpty(content.Error))
            {
                text = "Could not load the ship: " + content.Error;
                variation = "ErrorLabel";
            }
            else if (!content.IsFinished)
            {
                int total = content.TotalChunks;
                if (total < 0) text = "Reading ship...";
                else if (content.MeshedChunks < total) text = $"Building mesh {content.MeshedChunks}/{total} chunks";
            }
        }

        _contentInfo.Text = text;
        _contentInfo.ThemeTypeVariation = variation;
        _contentInfo.Visible = text.Length > 0;
    }
}
