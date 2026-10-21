using System.Text.Json;
using System.Text.Json.Nodes;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.UI.Panels;

public sealed class BlockEntryPreview : IEntryPreview
{
    private bool _inactive;

    public bool CanPreview(EditorContext context, EditorEntry entry)
    {
        return context.Assets.TryGetBlock(IdOf(entry), out _);
    }

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
        ImGui.Checkbox("Inactive textures", ref _inactive);
        if (entry.File.IsDirty)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(save and reload to update)");
        }
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
        if (!context.Assets.TryGetBlock(IdOf(entry), out var block)) return;
        var state = _inactive ? BlockState.Inactive : BlockState.Active;
        viewport.SetContent((block.Id, state), context.Assets.Version, () => new BlockPreviewContent(block, state));
    }

    internal static string IdOf(EditorEntry entry)
    {
        return entry.Node is JsonObject obj ? EditorJson.GetString(obj, "ID") : "";
    }
}

public sealed class ItemEntryPreview : IEntryPreview
{
    public bool CanPreview(EditorContext context, EditorEntry entry)
    {
        return context.Assets.TryGetItem(BlockEntryPreview.IdOf(entry), out _);
    }

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
        if (entry.File.IsDirty) ImGui.TextDisabled("(save and reload to update)");
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
        var id = BlockEntryPreview.IdOf(entry);
        if (context.Assets.TryGetBlock(id, out var block) && block.Id > 1)
        {
            viewport.SetContent(("block", block.Id), context.Assets.Version, () => new BlockPreviewContent(block, BlockState.Active));
            return;
        }

        if (!context.Assets.TryGetItem(id, out var item)) return;
        viewport.SetContent(("item", item.Id), context.Assets.Version, () => new ItemPreviewContent(GameAssets.GetItemWorldModelById(item.Id)));
    }
}

public sealed class ProjectileEntryPreview : IEntryPreview
{
    public bool CanPreview(EditorContext context, EditorEntry entry)
    {
        return entry.Node is JsonObject;
    }

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
        var obj = (JsonObject)entry.Node;
        double speed = EditorJson.GetNumber(obj, "Speed", 5);
        double range = EditorJson.GetNumber(obj, "MaxTravelDistance", 100);
        double flight = speed > 0 ? range / speed : 0;
        ImGui.TextDisabled($"Flight time {flight:0.00}s, damage {EditorJson.GetNumber(obj, "Damage"):0} / blocks {EditorJson.GetNumber(obj, "DamageBlocks"):0}. White line = 1 block");
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
        viewport.SetContent(entry.Key, 0, () => new ProjectilePreviewContent());
        if (viewport.Content is not ProjectilePreviewContent content || entry.Node is not JsonObject obj) return;

        content.Speed = (float)EditorJson.GetNumber(obj, "Speed", 5);
        content.Length = (float)EditorJson.GetNumber(obj, "Length", 1);
        content.Thickness = (float)EditorJson.GetNumber(obj, "Thickness", 0.2);
        var color = EditorJson.Get(obj, "Color") as JsonObject;
        content.Color = color == null
            ? Color4.Blue
            : new Color4((float)EditorJson.GetNumber(color, "r") / 255f, (float)EditorJson.GetNumber(color, "g") / 255f, (float)EditorJson.GetNumber(color, "b") / 255f, 1f);
    }
}

public sealed class AsteroidEntryPreview : IEntryPreview
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private int _radius = 32;
    private int _seed = 12345;
    private int _generation;
    private string _lastEntry;

    public bool CanPreview(EditorContext context, EditorEntry entry)
    {
        return entry.Node is JsonObject && context.Assets.IsLoaded;
    }

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
        var obj = (JsonObject)entry.Node;
        int min = Math.Max(2, (int)EditorJson.GetNumber(obj, "MinRadius", 8));
        int max = Math.Max(min, (int)EditorJson.GetNumber(obj, "MaxRadius", 32));

        if (_lastEntry != entry.Key)
        {
            _lastEntry = entry.Key;
            _radius = (min + max) / 2;
            _generation++;
        }

        _radius = Math.Clamp(_radius, min, max);
        float width = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width * 0.35f);
        ImGui.SliderInt("Radius", ref _radius, min, max);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(width * 0.25f);
        ImGui.InputInt("Seed", ref _seed);
        ImGui.SameLine();
        if (ImGui.Button("Generate")) _generation++;
        ImGui.SameLine();
        if (ImGui.Button("Random")) { _seed = Random.Shared.Next(); _generation++; }
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
        var key = (entry.Key, _generation);
        viewport.SetContent(key, context.Assets.Version, () => Create(entry));

        if (viewport.Content is AsteroidPreviewContent asteroid) DrawStatistics(context, asteroid);
    }

    private IPreviewContent Create(EditorEntry entry)
    {
        var json = entry.Node.Deserialize<AsteroidJSON>(Options);
        if (json == null) return null;
        json.WormSettings ??= new WormSettingsJSON();

        var data = GenerationLoader.ConvertAsteroid(json);
        return data == null ? null : new AsteroidPreviewContent(data, _radius, _seed);
    }

    private static void DrawStatistics(EditorContext context, AsteroidPreviewContent asteroid)
    {
        if (!asteroid.IsFinished)
        {
            ImGui.ProgressBar((float)asteroid.GeneratedChunks / Math.Max(1, asteroid.TotalChunks), new System.Numerics.Vector2(-1f, 0f), $"{asteroid.GeneratedChunks}/{asteroid.TotalChunks} chunks");
            return;
        }

        var counts = asteroid.GetBlockCounts();
        long total = 0;
        foreach (var count in counts.Values) total += count;
        if (total == 0) return;

        float iconSize = ImGui.GetTextLineHeight();
        int shown = 0;
        foreach (var pair in counts.OrderByDescending(p => p.Value))
        {
            if (shown++ >= 8) break;
            var block = GameAssets.GetBlockDataById(pair.Key);
            if (shown > 1) ImGui.SameLine();
            EditorWidgets.Icon(block?.AsItem?.Icon ?? default, iconSize);
            EditorWidgets.Tooltip(block?.Name);
            ImGui.SameLine();
            ImGui.TextDisabled($"{pair.Value * 100.0 / total:0.#}%");
        }
    }
}

public sealed class MobEntryPreview : IEntryPreview
{
    private static readonly string[] Actions = { "Idle", "Eating" };

    private int _action;
    private bool _showRanges = true;
    private bool _showBlock = true;
    private bool _bite;
    private bool _hurt;

    public bool CanPreview(EditorContext context, EditorEntry entry)
    {
        return GameAssets.Mobs.ContainsKey(IdOf(entry));
    }

    public void DrawControls(EditorContext context, EditorEntry entry)
    {
        float width = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width * 0.25f);
        ImGui.Combo("Action", ref _action, Actions, Actions.Length);
        ImGui.SameLine();
        _bite |= ImGui.Button("Bite");
        ImGui.SameLine();
        _hurt |= ImGui.Button("Hurt");
        ImGui.SameLine();
        ImGui.Checkbox("Ranges", ref _showRanges);
        EditorWidgets.Tooltip("Green: hit radius, red: attack range");
        ImGui.SameLine();
        ImGui.Checkbox("Block", ref _showBlock);
        EditorWidgets.Tooltip("1x1x1 block for size reference");
        if (entry.File.IsDirty) ImGui.TextDisabled("(model and texture: save and reload to update)");
    }

    public void Apply(EditorContext context, EditorEntry entry, PreviewViewport viewport)
    {
        if (!GameAssets.Mobs.TryGetValue(IdOf(entry), out var definition) || entry.Node is not JsonObject obj) return;

        float scale = MathF.Max(0.05f, (float)EditorJson.GetNumber(obj, "Scale", definition.Scale));
        float radius = Math.Clamp((float)EditorJson.GetNumber(obj, "Radius", definition.Radius), 0.1f, 0.49f);
        float attackRange = MathF.Max(0.5f, (float)EditorJson.GetNumber(obj, "AttackRange", definition.AttackRange));

        viewport.SetContent((definition.Id, scale, radius, attackRange), context.Assets.Version,
            () => new MobPreviewContent(definition, scale, radius, attackRange));
        if (viewport.Content is not MobPreviewContent mob) return;

        mob.Action = _action == 1 ? Spacebox.Game.Mobs.MobAction.Eating : Spacebox.Game.Mobs.MobAction.Idle;
        mob.ShowRanges = _showRanges;
        mob.ShowBlock = _showBlock;
        if (_bite) mob.PlayBite();
        if (_hurt) mob.PlayHurt();
        _bite = _hurt = false;
    }

    private static string IdOf(EditorEntry entry)
    {
        return entry.Node is JsonObject obj ? EditorJson.GetString(obj, "Id") : "";
    }
}
