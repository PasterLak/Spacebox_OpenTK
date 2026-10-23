using System.Diagnostics;
using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Data;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Game;
using Spacebox.Game.Resource.Content.Conversion;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class OverviewPage : EditorPage
{
    private readonly FieldWidgetRegistry _widgets;
    private readonly GridContainer _stats = new GridContainer { Columns = 2 };
    private readonly TextureRect _icon = new TextureRect { CustomMinimumSize = new Vector2(160f, 160f), StretchMode = TextureStretchMode.KeepAspectCentered, MouseFilter = MouseFilter.Ignore };
    private JsonDocumentView _config;
    private Button _convert;
    private Texture2D _iconTexture;
    private string _convertTooltip;

    public OverviewPage(FieldWidgetRegistry widgets)
    {
        _widgets = widgets;
    }

    public override string Title => "Overview";
    public override string Description => "GameSet info, config.json and the loaded content counts.";

    protected override void Build()
    {
        var split = AddChild(new SplitContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand, MinFirst = 220f });
        split.Ratio = 0.3f;

        var info = split.AddChild(new VBoxContainer());
        info.AddChild(_icon);
        var project = Editor.Project;
        info.AddChild(new Label(project.RootPath) { Autowrap = true, FontColor = EditorCanvasTheme.TextDim });
        var open = info.AddChild(new Button("Open folder") { TooltipText = "Opens the GameSet folder in the file explorer.", SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
        open.Pressed += _ => OpenFolder(project.RootPath);

        if (project.Mirror != null)
        {
            info.AddChild(new Label("Development build: saves are also written to") { FontColor = EditorCanvasTheme.Success });
            info.AddChild(new Label(project.Mirror.Root) { Autowrap = true, FontColor = EditorCanvasTheme.TextDim, MouseFilter = MouseFilter.Stop, TooltipText = "Debug and Release builds run from bin/, which a rebuild overwrites. Every save and delete is copied to the source GameSet folder so your changes are kept." });
        }

        _convertTooltip = "Moves every block and item into its own folder with its textures, blueprints into the entry folders and processing recipes into Recipes/. The loaded content stays identical." + (project.Mirror != null ? " The source GameSet folder is converted too." : "");
        _convert = info.AddChild(new Button("Convert to folder format") { SizeFlagsHorizontal = SizeFlags.ShrinkBegin });
        _convert.Pressed += _ => Convert();
        info.AddChild(new HSeparator());
        info.AddChild(_stats);
        info.AddChild(new Label("Counts show the last loaded state.") { FontColor = EditorCanvasTheme.TextDim });

        var right = split.AddChild(new VBoxContainer());
        right.AddChild(new Label("config.json") { FontColor = EditorCanvasTheme.Accent });
        _config = right.AddChild(new JsonDocumentView(_widgets));
        _config.BindFile(Editor, project.GetFile(GameSetCatalog.ConfigFile), GameSetCatalog.FileSchema(GameSetCatalog.ConfigFile), EditorScopes.Config);
        LoadIcon();
    }

    protected override void OnShown() => FillStatistics();

    public override void Tick(float deltaTime)
    {
        bool flat = GameSetFolderConverter.HasFlatFiles(Editor.Project.RootPath);
        bool dirty = Editor.Project.DirtyCount > 0;
        _convert.Visible = flat && !Editor.Project.IsReadOnly;
        _convert.Disabled = dirty;
        _convert.TooltipText = dirty ? "Save or discard your changes first" : _convertTooltip;
    }

    public override void Release()
    {
        _iconTexture?.Dispose();
        _iconTexture = null;
    }

    private void FillStatistics()
    {
        _stats.ClearChildren();
        Row("Blocks", GameAssets.Blocks.Count);
        Row("Items", GameAssets.Items.Count);
        Row("Projectiles", GameAssets.Projectiles.Count);
        Row("Blueprints", GameAssets.Blueprints.Count);
        Row("Recipe types", GameAssets.Recipes.Count);
        Row("Mobs", GameAssets.Mobs.Count);
        Row("Sounds", GameAssets.Sounds.Count);
        Row("Block textures", GameAssets.AtlasBlocks?.Count ?? 0);
        Row("Item sprites", GameAssets.AtlasItems?.Count ?? 0);
    }

    private void Row(string label, int value)
    {
        _stats.AddChild(new Label(label) { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
        _stats.AddChild(new Label(value.ToString()) { HorizontalAlignment = HorizontalAlignment.Right });
    }

    private void LoadIcon()
    {
        string path = Path.Combine(Editor.Project.RootPath, "icon.png");
        if (!File.Exists(path)) return;
        _iconTexture = new Texture2D(path, true, true);
        _icon.Texture = _iconTexture;
    }

    private void Convert()
    {
        var project = Editor.Project;
        try
        {
            var result = ProjectFolderConversion.Convert(project.RootPath, project.Mirror?.Root);
            project.ReloadFromDisk();
            Editor.SetStatus(result.Success ? $"Converted: {result.Root.Summary}" : "Conversion failed: " + string.Join("; ", result.Problems.Take(3)), !result.Success);
            foreach (var line in result.Log) Engine.Debug.Log($"[Editor] {line}");
            if (result.Root.Success) Editor.RequestReload();
        }
        catch (Exception ex)
        {
            Engine.Debug.Error($"[Editor] Conversion failed: {ex}");
            Editor.SetStatus("Conversion failed: " + ex.Message, true);
        }
    }

    private static void OpenFolder(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Engine.Debug.Error($"[Editor] Cannot open folder: {ex.Message}");
        }
    }
}
