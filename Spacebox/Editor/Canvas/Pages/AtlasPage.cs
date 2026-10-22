using Engine;
using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Preview;
using Spacebox.Game;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class AtlasPage : EditorPage
{
    private static readonly string[] Views = { "Blocks", "Blocks emission", "Items", "Items emission" };

    private readonly LineEdit _filter = new LineEdit { PlaceholderText = "Search...", TooltipText = "Show only textures whose name contains this text." };
    private readonly ItemList _regions = new ItemList { Name = "Regions", IconSize = 18f, SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly ComboBox _view = new ComboBox { Editable = false, CustomMinimumSize = new Vector2(150f, 0f), TooltipText = "Which texture atlas to show: block or item textures, or their glow (emission) layers." };
    private readonly HSlider _zoom = new HSlider { MinValue = AtlasView.MinZoom, MaxValue = AtlasView.MaxZoom, CustomMinimumSize = new Vector2(140f, 0f), TooltipText = "Zoom of the atlas image. The mouse wheel over the atlas zooms too, drag moves it." };
    private readonly Label _zoomLabel = new Label { VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(48f, 0f) };
    private readonly CheckBox _fit = new CheckBox("Fit", true) { TooltipText = "Zooms the atlas so it always fits the window." };
    private readonly Label _info = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly AtlasView _canvas = new AtlasView { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private TextureAtlas _shownAtlas;
    private string _shownFilter;
    private int _shownVersion = -1;
    private bool _syncing;

    public override string Title => "Atlases";
    public override string Description => "The texture atlases the game built from this GameSet.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 160f, MinSecond = 260f });
        split.Ratio = 0.22f;
        var left = split.AddChild(new VBoxContainer());
        left.AddChild(_filter);
        left.AddChild(_regions);
        _regions.ItemSelected += index => _canvas.SelectedRegion = _regions.Items[index].Metadata as string;

        var right = split.AddChild(new VBoxContainer());
        var toolbar = right.AddChild(new HBoxContainer());
        var options = new List<ListItem>();
        foreach (var view in Views) options.Add(new ListItem { Text = view });
        _view.SetOptions(options);
        _view.Value = Views[0];
        toolbar.AddChild(_view);
        toolbar.AddChild(_zoomLabel);
        toolbar.AddChild(_zoom);
        _zoom.ValueChanged += value =>
        {
            if (_syncing) return;
            _canvas.Zoom = (float)value;
            _canvas.Fit = false;
        };
        toolbar.AddChild(_fit);
        _fit.Toggled += (_, pressed) => _canvas.Fit = pressed;
        var reload = toolbar.AddChild(new Button("Reload GameSet") { TooltipText = "Rebuilds the atlases from the files on disk." });
        reload.Pressed += _ => Editor.RequestReload();
        right.AddChild(_info);
        right.AddChild(_canvas);
        _canvas.RegionClicked += SelectInList;
    }

    public override void Tick(float deltaTime)
    {
        int view = Array.IndexOf(Views, _view.Value);
        var atlas = view < 2 ? GameAssets.AtlasBlocks : GameAssets.AtlasItems;
        _canvas.Atlas = atlas;
        _canvas.Texture = view switch
        {
            0 => GameAssets.BlocksTexture,
            1 => GameAssets.EmissionBlocks,
            2 => GameAssets.ItemsTexture,
            _ => GameAssets.EmissionItems
        };

        _info.Text = _canvas.Texture == null || atlas == null ? "Atlas not loaded" : $"{_canvas.Texture.Width}x{_canvas.Texture.Height} px, {atlas.Count} textures";
        if (_fit.ButtonPressed != _canvas.Fit) _fit.ButtonPressed = _canvas.Fit;
        _syncing = true;
        _zoom.Value = _canvas.Zoom;
        _syncing = false;
        _zoomLabel.Text = $"{_canvas.Zoom:0.00}x";

        if (!ReferenceEquals(atlas, _shownAtlas) || _filter.Text != _shownFilter || _shownVersion != Editor.Assets.Version) RefreshRegions(atlas);
    }

    private void RefreshRegions(TextureAtlas atlas)
    {
        _shownAtlas = atlas;
        _shownFilter = _filter.Text;
        _shownVersion = Editor.Assets.Version;
        _regions.Clear();
        if (atlas == null) return;

        var names = new List<string>(atlas.Names);
        names.Sort(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(_shownFilter) && name.IndexOf(_shownFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            _regions.AddItem(name, null, name).WithIcon(atlas.GetSprite(name));
            if (name == _canvas.SelectedRegion) _regions.Select(_regions.Items.Count - 1);
        }
    }

    private void SelectInList(string name)
    {
        int index = _regions.IndexOf(item => item.Metadata as string == name);
        if (index < 0) return;
        _regions.Select(index);
        _regions.EnsureVisible(index);
    }
}
