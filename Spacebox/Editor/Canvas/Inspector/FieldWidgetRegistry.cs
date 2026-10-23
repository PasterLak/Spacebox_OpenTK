using System.Text.Json.Nodes;
using Engine.CanvasUI;
using Spacebox.Editor.Data;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Game;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.Canvas.Inspector;

public sealed class FieldWidgetRegistry
{
    private static readonly string[] EquipmentSlots = Enum.GetNames(typeof(EquipmentSlotType));

    private readonly List<IFieldWidget> _widgets = new List<IFieldWidget>();
    private readonly List<(Func<FieldContext, bool> Matches, Func<EditorContext, IReadOnlyList<string>> Keys)> _keySuggestions = new();
    private readonly IFieldWidget _fallback = new PrimitiveFieldWidget();

    public static FieldWidgetRegistry CreateDefault()
    {
        var widgets = new FieldWidgetRegistry();
        widgets.Add(new ColorFieldWidget());
        widgets.Add(new HexColorFieldWidget(c => c.Scope == EditorScopes.GameModes && c.NameIs("TagColor") && IsTopLevel(c)));
        widgets.Add(new VectorFieldWidget());
        widgets.Add(new NumberRangeFieldWidget(c => c.Scope == EditorScopes.Asteroids && c.NameIs("MinRadius", "MaxRadius"), AsteroidRadius.Min, AsteroidRadius.Max));

        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Textures"), e => e.Assets.BlockTextures, (e, n) => e.Assets.BlockTexture(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Sprite"), e => e.Assets.ItemSprites, (e, n) => e.Assets.ItemSprite(n)));
        widgets.Add(new ReferenceFieldWidget(IsItemReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(IsBlockReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Projectile"), e => e.Assets.Projectiles));
        widgets.Add(new ReferenceFieldWidget(IsSoundReference, e => e.Assets.Sounds));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.NameIs("Type") && IsTopLevel(c), _ => GameSetCatalog.BlockTypeNames.OrderBy(t => t).ToArray()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("Type") && IsTopLevel(c), _ => GameSetCatalog.ItemTypeNames.ToArray()));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Category") && IsTopLevel(c), e => e.Assets.Categories));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("Slot"), _ => EquipmentSlots));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Mob"), e => e.Assets.Mobs));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Loot"), e => e.Assets.LootTables));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Generator && c.PathContains("Biomes"), e => e.Assets.Biomes));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("FallbackBiome", "PreferredSpawnBiome") || c.Scope == EditorScopes.Asteroids && c.PathContains("Mobs") && c.PathContains("Biomes"), e => e.Assets.Biomes));
        widgets.Add(new ReferenceFieldWidget(c => c.PathContains("ExcludedAsteroids"), e => e.Assets.Asteroids));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("PutIn"), _ => Spacebox.Game.Player.StartItemPlacement.All));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.GameModes && c.NameIs("Base") && IsTopLevel(c), _ => GameModeBases));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Type"), _ => Spacebox.Game.Tutorial.Tasks.TutorialTaskFactory.Types.OrderBy(t => t).ToArray()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Action"), _ => InputActions()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Block"), e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));

        widgets.AddKeySuggestions(c => c.Scope == EditorScopes.Biomes && c.NameIs("Asteroids"), e => e.Assets.Asteroids);
        widgets.AddKeySuggestions(c => c.NameIs("BiomeMultipliers"), e => e.Assets.Biomes);
        return widgets;
    }

    private static readonly string[] GameModeBases = { "survival", "creative", "spectator" };

    public void Add(IFieldWidget widget) => _widgets.Add(widget);

    public void AddKeySuggestions(Func<FieldContext, bool> matches, Func<EditorContext, IReadOnlyList<string>> keys) => _keySuggestions.Add((matches, keys));

    public IReadOnlyList<string> KeySuggestions(FieldContext context)
    {
        foreach (var (matches, keys) in _keySuggestions)
            if (matches(context)) return keys(context.Editor);
        return Array.Empty<string>();
    }

    public bool IsInline(FieldContext context) => context.Value is JsonValue || context.Value == null || Find(context) != null;

    public Control Create(FieldContext context, Action<JsonNode> commit) => (Find(context) ?? _fallback).Create(context, commit);

    private IFieldWidget Find(FieldContext context)
    {
        foreach (var widget in _widgets)
            if (widget.CanEdit(context)) return widget;
        return null;
    }

    private static string[] InputActions()
    {
        var actions = Engine.InputPro.InputManager.Instance?.GetAllActions();
        if (actions == null) return Array.Empty<string>();

        var names = new List<string>(actions.Count);
        foreach (var (id, _) in actions) names.Add(id);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names.ToArray();
    }

    private static bool IsTopLevel(FieldContext context) => context.Path.IndexOf('.') < 0;

    private static bool IsItemReference(FieldContext context)
    {
        if (context.NameIs("Item", "Drop", "Ammo", "DnaItem")) return true;
        if (context.Scope == EditorScopes.Loot && context.NameIs("ID")) return true;
        if (context.Scope == EditorScopes.Crafting && context.NameIs("Icon")) return true;
        return context.NameIs("ID") && (context.PathContains("ItemsOnStart") || context.PathContains("GiveItemsOnStart"));
    }

    private static bool IsBlockReference(FieldContext context)
    {
        if (context.NameIs("FillBlockID", "BlockId")) return true;
        return context.Scope == EditorScopes.Mobs && context.PathContains("EdibleBlocks");
    }

    private static bool IsSoundReference(FieldContext context)
    {
        if (context.NameIs("Sound", "ShotSound", "UseSound")) return true;
        return context.Scope == EditorScopes.Blocks && context.PathContains("Sounds");
    }
}
