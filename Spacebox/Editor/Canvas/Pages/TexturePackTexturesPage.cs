using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class TexturePackTexturesPage : EditorPage
{
    private const float ImageSize = 192f;
    private static readonly string[] Kinds = { "All", "Blocks", "Items", "Glow" };

    private readonly LineEdit _filter = new LineEdit { PlaceholderText = "Search...", TooltipText = "Show only textures whose name contains this text." };
    private readonly ComboBox _kind = new ComboBox { Editable = false, TooltipText = "Block textures, item textures or glow (emission) textures." };
    private readonly CheckBox _onlyPack = new CheckBox("Only in pack", false) { TooltipText = "Show only the textures this pack replaces." };
    private readonly ItemList _list = new ItemList { Name = "Textures", SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private readonly Label _name = new Label { FontColor = EditorCanvasTheme.Accent };
    private readonly Label _state = new Label { Autowrap = true, FontColor = EditorCanvasTheme.TextDim };
    private readonly TextureRect _baseImage = Image("The texture of the GameSet.");
    private readonly TextureRect _packImage = Image("The texture of this pack that replaces it.");
    private readonly Button _copy = new Button("Copy into pack") { TooltipText = "Copies the GameSet texture into the pack, so you can paint over it." };
    private readonly Button _open = new Button("Open in image editor") { TooltipText = "Opens the pack's file with the program your system uses for images. Press Reload after saving it." };
    private readonly Button _remove = new Button("Remove from pack") { TooltipText = "Deletes the pack's file, so the GameSet texture is used again." };
    private List<PackTextureRow> _rows = new List<PackTextureRow>();
    private PackTextureRow _selected;
    private Texture2D _baseTexture;
    private Texture2D _packTexture;
    private string _shownFilter;
    private string _shownKind;
    private bool _shownOnlyPack;
    private int _shownVersion = -1;
    private bool _confirmRemove;

    public override string Title => "Textures";
    public override string Description => "Every texture of the GameSet and the ones this pack replaces.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 180f, MinSecond = 320f });
        split.Ratio = 0.28f;
        var left = split.AddChild(new VBoxContainer());
        left.AddChild(_filter);
        var options = new List<ListItem>();
        foreach (var kind in Kinds) options.Add(new ListItem { Text = kind });
        _kind.SetOptions(options);
        _kind.Value = Kinds[0];
        var row = left.AddChild(new HBoxContainer());
        row.AddChild(_kind);
        row.AddChild(_onlyPack);
        left.AddChild(_list);
        _list.ItemSelected += index => Select(_list.Items[index].Metadata as PackTextureRow);

        var right = split.AddChild(new VBoxContainer());
        right.AddChild(_name);
        right.AddChild(_state);
        var images = right.AddChild(new HBoxContainer());
        images.AddChild(Column("GameSet", _baseImage));
        images.AddChild(Column("Pack", _packImage));
        var buttons = right.AddChild(new HBoxContainer());
        buttons.AddChild(_copy);
        buttons.AddChild(_open);
        buttons.AddChild(_remove);
        var folder = buttons.AddChild(new Button("Open pack folder") { TooltipText = "Opens the pack folder in the file manager." });
        _copy.Pressed += _ => Copy();
        _open.Pressed += _ => Open();
        _remove.Pressed += _ => Remove();
        folder.Pressed += _ => Application.OpenLink(Editor.TexturePackFolder);
        right.AddChild(new Label("Pack files use the GameSet texture's file name: Blocks/<name>.png, Items/<name>.png, Emissions/<name>.png.") { Autowrap = true, FontColor = EditorCanvasTheme.TextDim });
        Select(null);
    }

    public override void Tick(float deltaTime)
    {
        if (_shownVersion == Editor.Assets.Version && _filter.Text == _shownFilter && _kind.Value == _shownKind && _onlyPack.ButtonPressed == _shownOnlyPack) return;
        Refresh();
    }

    public override void Hide()
    {
        base.Hide();
        ReleaseTextures();
    }

    public override void Release()
    {
        base.Release();
        ReleaseTextures();
    }

    protected override void OnShown()
    {
        base.OnShown();
        _shownVersion = -1;
    }

    private void Refresh()
    {
        _shownVersion = Editor.Assets.Version;
        _shownFilter = _filter.Text;
        _shownKind = _kind.Value;
        _shownOnlyPack = _onlyPack.ButtonPressed;

        _rows = PackTextures.Build(Editor.BaseTextures, TexturePack.Load(Editor.TexturePackFolder));
        _list.Clear();
        int kindIndex = Array.IndexOf(Kinds, _shownKind);
        PackTextureRow reselect = null;
        foreach (var row in _rows)
        {
            if (kindIndex > 0 && (int)row.Kind != kindIndex - 1) continue;
            if (_shownOnlyPack && !row.InPack) continue;
            if (!string.IsNullOrEmpty(_shownFilter) && row.Name.IndexOf(_shownFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            string marker = row.Unused ? "  (unused)" : row.InPack ? "  *" : "";
            _list.AddItem($"{KindName(row.Kind)}: {row.Name}{marker}", null, row);
            if (_selected != null && row.Kind == _selected.Kind && string.Equals(row.Name, _selected.Name, StringComparison.OrdinalIgnoreCase))
            {
                reselect = row;
                _list.Select(_list.Items.Count - 1);
            }
        }
        Select(reselect);
    }

    private void Select(PackTextureRow row)
    {
        _selected = row;
        _confirmRemove = false;
        _remove.Text = "Remove from pack";
        ReleaseTextures();

        _name.Text = row == null ? "Pick a texture" : $"{KindName(row.Kind)}: {row.Name}";
        _state.Text = row == null ? "" : row.Unused
            ? "The pack has this file, but the GameSet has no texture with this name, so it is not used."
            : row.InPack ? "Replaced by this pack." : "Not in the pack: the GameSet texture is used.";
        _baseTexture = Load(row?.BasePath);
        _packTexture = Load(row?.PackPath);
        _baseImage.Texture = _baseTexture;
        _packImage.Texture = _packTexture;
        _copy.Disabled = row == null || row.InPack || row.Unused;
        _open.Disabled = row == null || !row.InPack;
        _remove.Disabled = row == null || !row.InPack;
    }

    private void Copy()
    {
        if (_selected == null || _selected.InPack || _selected.Unused) return;
        string target = PackTextures.TargetPath(Editor.TexturePackFolder, _selected);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(_selected.BasePath, target, false);
            Editor.Project.Mirror?.Copy(Path.GetRelativePath(Editor.TexturePackFolder, target), target);
            Editor.SetStatus($"Copied {_selected.Name} into the pack");
            Editor.RequestReload();
        }
        catch (Exception ex)
        {
            Editor.SetStatus("Copy failed: " + ex.Message, true);
        }
    }

    private void Open()
    {
        if (_selected?.PackPath != null) Application.OpenLink(_selected.PackPath);
    }

    private void Remove()
    {
        if (_selected?.PackPath == null) return;
        if (!_confirmRemove)
        {
            _confirmRemove = true;
            _remove.Text = "Click again to remove";
            return;
        }

        try
        {
            ReleaseTextures();
            File.Delete(_selected.PackPath);
            Editor.Project.Mirror?.Delete(Path.GetRelativePath(Editor.TexturePackFolder, _selected.PackPath));
            Editor.SetStatus($"Removed {_selected.Name} from the pack");
            Editor.RequestReload();
        }
        catch (Exception ex)
        {
            Editor.SetStatus("Remove failed: " + ex.Message, true);
        }
    }

    private void ReleaseTextures()
    {
        _baseImage.Texture = null;
        _packImage.Texture = null;
        _baseTexture?.Dispose();
        _packTexture?.Dispose();
        _baseTexture = null;
        _packTexture = null;
    }

    private static Texture2D Load(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            return new Texture2D(path, true, true);
        }
        catch (Exception ex)
        {
            Engine.Debug.Warning($"[Editor] Could not load {path}: {ex.Message}");
            return null;
        }
    }

    private static string KindName(PackTextureKind kind) => kind switch
    {
        PackTextureKind.Block => "Block",
        PackTextureKind.Item => "Item",
        _ => "Glow"
    };

    private static TextureRect Image(string tooltip) => new TextureRect
    {
        CustomMinimumSize = new Vector2(ImageSize, ImageSize),
        StretchMode = TextureStretchMode.KeepAspectCentered,
        TooltipText = tooltip
    };

    private static Control Column(string title, TextureRect image)
    {
        var column = new VBoxContainer();
        column.AddChild(new Label(title) { FontColor = EditorCanvasTheme.TextDim });
        var frame = column.AddChild(new ColorRect { Color = EditorCanvasTheme.Field, CustomMinimumSize = new Vector2(ImageSize, ImageSize) });
        image.SetAnchorsPreset(LayoutPreset.FullRect);
        frame.AddChild(image);
        return column;
    }
}
