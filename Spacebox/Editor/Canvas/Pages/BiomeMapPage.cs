using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Preview;
using Spacebox.Editor.Generation;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class BiomeMapPage : EditorPage
{
    private readonly BiomeMapPreview _map = new BiomeMapPreview();
    private readonly LineEdit _seed = new LineEdit { Text = "spacebox", TooltipText = "World seed (text or number), the same as in Create World. The same seed always gives the same map." };
    private readonly NumberEdit[] _sector = new NumberEdit[3];
    private readonly CheckBox _legacy = new CheckBox("Old worlds (generator 1)") { TooltipText = "Show the map as worlds made before 0.1.5 generate it (no distance rules, biome density or shapes)." };
    private readonly Button _build = new Button("Build") { TooltipText = "Generates the biome map and asteroid positions of the sector with the saved GameSet files." };
    private readonly HSlider _slice = new HSlider { MinValue = 0, MaxValue = BiomesMap.Resolution - 1, Step = 1, Value = 64, TooltipText = "Height of the horizontal cut through the sector that the map shows (0 = bottom, 127 = top, 64 blocks per step)." };
    private readonly Label _sliceLabel = new Label();
    private readonly CheckBox _rings = new CheckBox("Distance rings", true) { TooltipText = "Rings show where biomes may start and end: each ring is a biome MinDistanceFromCenter or MaxDistanceFromCenter, measured from the world center (0,0,0) and cut by this height slice. The ring color is the biome color. Biomes without distance limits have no rings." };
    private readonly Label _count = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly VBoxContainer _legend = new VBoxContainer();
    private BiomeMapView _view;
    private int _legendVersion = -1;

    public override string Title => "Biome map";
    public override string Description => "Biome map and asteroid positions of one sector.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 220f, MinSecond = 200f });
        split.Ratio = 0.3f;
        var scroll = split.AddChild(new ScrollContainer());
        var controls = scroll.AddChild(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        controls.AddChild(new Label("Builds the biome map of one sector with the loaded GameSet. Dots are asteroid positions (bright: inside the shown slice).") { Autowrap = true, FontColor = EditorCanvasTheme.TextDim });
        controls.AddChild(new HSeparator());
        controls.AddChild(new Label("World seed"));
        controls.AddChild(_seed);
        controls.AddChild(new Label("Sector index"));
        var sector = controls.AddChild(new HBoxContainer());
        for (int i = 0; i < 3; i++)
        {
            _sector[i] = sector.AddChild(new NumberEdit { Integer = true, Value = 0, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, TooltipText = "Which sector to show (x, y, z). One sector is 8192 blocks per side; sector 0,0,0 starts at the world center." });
        }
        controls.AddChild(_legacy);
        controls.AddChild(_build);
        _build.Pressed += _ => BuildMap();
        controls.AddChild(_sliceLabel);
        controls.AddChild(_slice);
        controls.AddChild(_rings);
        controls.AddChild(_count);
        controls.AddChild(new HSeparator());
        controls.AddChild(_legend);

        _view = split.AddChild(new BiomeMapView(_map));
    }

    public override void Tick(float deltaTime)
    {
        _build.Disabled = _map.IsBuilding;
        _legacy.Disabled = _map.IsBuilding;
        _view.Slice = (int)_slice.Value;
        _view.ShowRings = _rings.ButtonPressed;
        _sliceLabel.Text = $"Height slice {(int)_slice.Value}";
        _count.Text = _map.HasMap ? $"{_map.AsteroidCount} asteroids in sector" : "";
        if (_legendVersion != Editor.Assets.Version) RebuildLegend();
    }

    public override void Release()
    {
        _map.Dispose();
    }

    private void BuildMap()
    {
        var index = new Vector3i((int)_sector[0].Value, (int)_sector[1].Value, (int)_sector[2].Value);
        _map.Build(_seed.Text, index, _legacy.ButtonPressed ? GeneratorVersions.Legacy : GeneratorVersions.Current);
    }

    private void RebuildLegend()
    {
        _legendVersion = Editor.Assets.Version;
        _legend.ClearChildren();
        var generator = World.WorldGenerator;
        if (generator?.Biomes == null) return;

        foreach (var biome in generator.Biomes)
        {
            if (biome == null) continue;
            var c = biome.DebugColor;
            var row = _legend.AddChild(new HBoxContainer());
            row.TooltipText = $"{biome.Description}\nAppears {DistanceText(biome.Distance)} from the world center, spawn chance {biome.SpawnChance}, density {biome.Density}%.";
            row.MouseFilter = MouseFilter.Stop;
            row.AddChild(new ColorRect { Color = new Color4(c.R / 255f, c.G / 255f, c.B / 255f, 1f), CustomMinimumSize = new Vector2(14f, 14f), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            row.AddChild(new Label($"{biome.Name}  ({biome.AsteroidChances.Count} asteroid types, {DistanceText(biome.Distance)})") { Autowrap = true, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore });
        }
    }

    private static string DistanceText(DistanceRange range)
    {
        if (range.IsUnlimited) return "everywhere";
        return range.Max == 0 ? $"from {range.Min} blocks" : $"{range.Min}-{range.Max} blocks";
    }
}
