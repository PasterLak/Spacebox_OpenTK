using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.Canvas.Preview;

public sealed class BlockPreviewView : PreviewEntryView
{
    private bool _inactive;

    public override bool Applies(EditorContext editor, EditorEntry entry) => editor.Assets.TryGetBlock(IdOf(entry), out _);

    protected override void BuildToolbar()
    {
        AddCheckBox("Inactive textures", false, "Show the textures the block uses when it is switched off or unpowered.", value => _inactive = value);
    }

    protected override void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport)
    {
        if (!editor.Assets.TryGetBlock(IdOf(entry), out var block)) return;
        var state = _inactive ? BlockState.Inactive : BlockState.Active;
        viewport.SetContent((block.Id, state), editor.Assets.Version, () => new BlockPreviewContent(block, state));
    }

    internal static string IdOf(EditorEntry entry) => entry?.Node is JsonObject obj ? EditorJson.GetString(obj, "ID") : "";
}

public sealed class ItemPreviewView : PreviewEntryView
{
    public override bool Applies(EditorContext editor, EditorEntry entry) => editor.Assets.TryGetItem(BlockPreviewView.IdOf(entry), out _);

    protected override void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport)
    {
        var id = BlockPreviewView.IdOf(entry);
        if (editor.Assets.TryGetBlock(id, out var block) && block.Id > 1)
        {
            viewport.SetContent(("block", block.Id), editor.Assets.Version, () => new BlockPreviewContent(block, BlockState.Active));
            return;
        }

        if (!editor.Assets.TryGetItem(id, out var item)) return;
        viewport.SetContent(("item", item.Id), editor.Assets.Version, () => new ItemPreviewContent(GameAssets.GetItemWorldModelById(item.Id)));
    }
}

public sealed class ProjectilePreviewView : PreviewEntryView
{
    private readonly Label _stats = new Label { FontColor = EditorCanvasTheme.TextDim };

    protected override bool ReadsRuntimeAssets => false;

    public override bool Applies(EditorContext editor, EditorEntry entry) => entry?.Node is JsonObject;

    protected override void BuildToolbar()
    {
        Extra.AddChild(_stats);
    }

    protected override void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport)
    {
        if (entry.Node is not JsonObject obj) return;
        double speed = EditorJson.GetNumber(obj, "Speed", 5);
        double range = EditorJson.GetNumber(obj, "MaxTravelDistance", 100);
        double flight = speed > 0 ? range / speed : 0;
        _stats.Text = $"Flight time {flight:0.00}s, damage {EditorJson.GetNumber(obj, "Damage"):0} / blocks {EditorJson.GetNumber(obj, "DamageBlocks"):0}. White line = 1 block";

        viewport.SetContent(entry.Key, 0, () => new ProjectilePreviewContent());
        if (viewport.Content is not ProjectilePreviewContent content) return;
        content.Speed = (float)speed;
        content.Length = (float)EditorJson.GetNumber(obj, "Length", 1);
        content.Thickness = (float)EditorJson.GetNumber(obj, "Thickness", 0.2);
        var color = EditorJson.Get(obj, "Color") as JsonObject;
        content.Color = color == null
            ? Color4.Blue
            : new Color4((float)EditorJson.GetNumber(color, "r") / 255f, (float)EditorJson.GetNumber(color, "g") / 255f, (float)EditorJson.GetNumber(color, "b") / 255f, 1f);
    }
}

public sealed class AsteroidPreviewView : PreviewEntryView
{
    private const int ShownBlocks = 8;
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private readonly HSlider _radius = new HSlider { Step = 1, MinValue = 2, MaxValue = 64, CustomMinimumSize = new Vector2(120f, 0f), TooltipText = "Radius of the preview asteroid in blocks (between MinRadius and MaxRadius)." };
    private readonly Label _radiusLabel = new Label { VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(70f, 0f) };
    private readonly NumberEdit _seed = new NumberEdit { Integer = true, Value = 12345, CustomMinimumSize = new Vector2(110f, 0f), TooltipText = "Random seed of the preview. The same seed always gives the same asteroid; in worlds every asteroid has its own seed." };
    private readonly Label _shape = new Label { FontColor = EditorCanvasTheme.TextDim };
    private readonly ProgressBar _progress = new ProgressBar { ShowPercentage = false };
    private readonly HBoxContainer _blocks = new HBoxContainer();
    private int _generation;
    private object _statisticsFor;

    protected override bool ReadsRuntimeAssets => false;

    public override bool Applies(EditorContext editor, EditorEntry entry) => entry?.Node is JsonObject && editor.Assets.IsLoaded;

    protected override void BuildToolbar()
    {
        Toolbar.AddChild(_radiusLabel);
        Toolbar.AddChild(_radius);
        _radius.ValueChanged += _ => Regenerate();
        Toolbar.AddChild(new Label("Seed") { VerticalAlignment = VerticalAlignment.Center });
        Toolbar.AddChild(_seed);
        _seed.ValueChanged += _ => Regenerate();
        AddButton("Generate", "Generates the asteroid again with the current settings and seed.", Regenerate);
        AddButton("Random", "Picks a new random seed: shows another asteroid of this type (another shape, size and ore layout).", () =>
        {
            _seed.Value = Random.Shared.Next();
            Regenerate();
        });
        Extra.AddChild(_shape);
        Extra.AddChild(_progress);
        Extra.AddChild(_blocks);
    }

    protected override void OnShow(EditorContext editor, EditorEntry entry)
    {
        var obj = (JsonObject)entry.Node;
        int min = Math.Max(2, (int)EditorJson.GetNumber(obj, "MinRadius", 8));
        int max = Math.Max(min, (int)EditorJson.GetNumber(obj, "MaxRadius", 32));
        _radius.MinValue = min;
        _radius.MaxValue = max;
        _radius.Value = (min + max) / 2;
        Regenerate();
    }

    protected override void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport)
    {
        _radiusLabel.Text = $"Radius {(int)_radius.Value}";
        var key = (entry.Key, _generation);
        viewport.SetContent(key, editor.Assets.Version, () => Create(entry));
        if (viewport.Content is not AsteroidPreviewContent asteroid)
        {
            _shape.Text = "";
            _progress.Visible = _blocks.Visible = false;
            return;
        }

        _shape.Text = asteroid.ShapeDescription ?? "";
        _progress.Visible = !asteroid.IsFinished;
        _blocks.Visible = asteroid.IsFinished;
        if (!asteroid.IsFinished)
        {
            _progress.MaxValue = Math.Max(1, asteroid.TotalChunks);
            _progress.Value = asteroid.GeneratedChunks;
            _progress.Text = $"{asteroid.GeneratedChunks}/{asteroid.TotalChunks} chunks";
            return;
        }

        if (ReferenceEquals(_statisticsFor, asteroid)) return;
        _statisticsFor = asteroid;
        ShowStatistics(asteroid);
    }

    private void Regenerate()
    {
        _generation++;
        _statisticsFor = null;
    }

    private IPreviewContent Create(EditorEntry entry)
    {
        var json = entry.Node.Deserialize<AsteroidJSON>(Options);
        if (json == null) return null;
        json.WormSettings ??= new WormSettingsJSON();

        var data = GenerationLoader.ConvertAsteroid(json);
        return data == null ? null : new AsteroidPreviewContent(data, (int)_radius.Value, (int)_seed.Value);
    }

    private void ShowStatistics(AsteroidPreviewContent asteroid)
    {
        _blocks.ClearChildren();
        var counts = asteroid.GetBlockCounts();
        long total = 0;
        foreach (var count in counts.Values) total += count;
        if (total == 0) return;

        var sorted = new List<KeyValuePair<short, long>>();
        foreach (var pair in counts) sorted.Add(new KeyValuePair<short, long>((short)pair.Key, pair.Value));
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (int i = 0; i < sorted.Count && i < ShownBlocks; i++)
        {
            var block = GameAssets.GetBlockDataById(sorted[i].Key);
            var icon = _blocks.AddChild(CanvasIcons.Image(block?.AsItem?.Icon ?? default, 16f));
            icon.TooltipText = block?.Name ?? "";
            _blocks.AddChild(new Label($"{sorted[i].Value * 100.0 / total:0.#}%  ") { FontColor = EditorCanvasTheme.TextDim, VerticalAlignment = VerticalAlignment.Center, TooltipText = block?.Name ?? "" });
        }
    }
}

public sealed class MobPreviewView : PreviewEntryView
{
    private readonly ComboBox _action = new ComboBox { Editable = false, CustomMinimumSize = new Vector2(100f, 0f), TooltipText = "Animation the preview mob plays." };
    private bool _showRanges = true;
    private bool _showBlock = true;
    private bool _bite;
    private bool _hurt;

    public override bool Applies(EditorContext editor, EditorEntry entry) => GameAssets.Mobs.ContainsKey(IdOf(entry));

    protected override void BuildToolbar()
    {
        _action.SetOptions(new List<ListItem> { new ListItem { Text = "Idle" }, new ListItem { Text = "Eating" } });
        _action.Value = "Idle";
        Toolbar.AddChild(_action);
        AddButton("Bite", "Plays the bite animation.", () => _bite = true);
        AddButton("Hurt", "Plays the hurt flash.", () => _hurt = true);
        AddCheckBox("Ranges", true, "Green: hit radius, red: attack range", value => _showRanges = value);
        AddCheckBox("Block", true, "1x1x1 block for size reference", value => _showBlock = value);
    }

    protected override void Apply(EditorContext editor, EditorEntry entry, PreviewViewport viewport)
    {
        if (!GameAssets.Mobs.TryGetValue(IdOf(entry), out var definition) || entry.Node is not JsonObject obj) return;

        float scale = MathF.Max(0.05f, (float)EditorJson.GetNumber(obj, "Scale", definition.Scale));
        float radius = Math.Clamp((float)EditorJson.GetNumber(obj, "Radius", definition.Radius), 0.1f, 0.49f);
        float attackRange = MathF.Max(0.5f, (float)EditorJson.GetNumber(obj, "AttackRange", definition.AttackRange));

        viewport.SetContent((definition.Id, scale, radius, attackRange), editor.Assets.Version,
            () => new MobPreviewContent(definition, scale, radius, attackRange));
        if (viewport.Content is not MobPreviewContent mob) return;

        mob.Action = _action.Value == "Eating" ? Spacebox.Game.Mobs.MobAction.Eating : Spacebox.Game.Mobs.MobAction.Idle;
        mob.ShowRanges = _showRanges;
        mob.ShowBlock = _showBlock;
        if (_bite) mob.PlayBite();
        if (_hurt) mob.PlayHurt();
        _bite = _hurt = false;
    }

    private static string IdOf(EditorEntry entry) => entry?.Node is JsonObject obj ? EditorJson.GetString(obj, "Id") : "";
}
