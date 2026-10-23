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
        widgets.Add(new HexColorFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Sign") && c.NameIs("TextColor")));
        widgets.Add(new VectorFieldWidget());
        widgets.Add(new NumberRangeFieldWidget(c => c.Scope == EditorScopes.Asteroids && c.NameIs("MinRadius", "MaxRadius"), AsteroidRadius.Min, AsteroidRadius.Max));

        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && (c.PathContains("Textures") || c.PathContains("Cable")), e => e.Assets.BlockTextures, (e, n) => e.Assets.BlockTexture(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Sprite"), e => e.Assets.ItemSprites, (e, n) => e.Assets.ItemSprite(n)));
        widgets.Add(new ReferenceFieldWidget(IsItemReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(IsBlockReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Projectile"), e => e.Assets.Projectiles));
        widgets.Add(new ReferenceFieldWidget(IsSoundReference, e => e.Assets.Sounds));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.NameIs("Type") && IsTopLevel(c), _ => EditorOptions.BlockTypes(), describe: Describe(FieldHelp.BlockTypes)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("Type") && IsTopLevel(c), _ => GameSetCatalog.ItemTypeNames.ToArray(), describe: Describe(FieldHelp.ItemTypes)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.NameIs("Model") && IsTopLevel(c), EditorOptions.BlockModels));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Components"), _ => EditorOptions.BlockComponents(), describe: Describe(FieldHelp.BlockComponents)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Processing") && c.NameIs("RecipeType"), e => e.Assets.RecipeTypes));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.NameIs("MoveDirection"), _ => EditorOptions.Directions));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && (c.PathContains("InputSides") || c.PathContains("OutputSides")), _ => EditorOptions.Directions));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Sign") && c.NameIs("Align"), _ => EditorOptions.SignAlignments));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Sign") && c.NameIs("Font"), _ => EditorOptions.Fonts()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Blocks && c.PathContains("Pictures"), e => e.Assets.PictureItems, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("ArmorModel"), EditorOptions.ProjectModels));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("ArmorTexture"), EditorOptions.ArmorTextures));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("Picture"), EditorOptions.Pictures));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Mobs && c.NameIs("Behaviour") && IsTopLevel(c), _ => EditorOptions.MobBehaviours(), describe: Describe(FieldHelp.MobBehaviours)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Mobs && c.NameIs("Model") && IsTopLevel(c), EditorOptions.MobModels));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Mobs && c.NameIs("Texture") && IsTopLevel(c), EditorOptions.MobTextures));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Config && c.NameIs("Extends") && IsTopLevel(c), e => e.Assets.GameSetIds));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Recipes && c.NameIs("Type"), e => e.Assets.RecipeTypes));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Category") && IsTopLevel(c), e => e.Assets.Categories));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Items && c.NameIs("Slot"), _ => EquipmentSlots));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Mob"), e => e.Assets.Mobs));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("Loot"), e => e.Assets.LootTables));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Generator && c.PathContains("Biomes"), e => e.Assets.Biomes));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("FallbackBiome", "PreferredSpawnBiome") || c.Scope == EditorScopes.Asteroids && c.PathContains("Mobs") && c.PathContains("Biomes"), e => e.Assets.Biomes));
        widgets.Add(new ReferenceFieldWidget(c => c.PathContains("ExcludedAsteroids"), e => e.Assets.Asteroids));
        widgets.Add(new ReferenceFieldWidget(c => c.NameIs("PutIn"), _ => Spacebox.Game.Player.StartItemPlacement.All));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.GameModes && c.NameIs("Base") && IsTopLevel(c), _ => GameModeBases));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Type"), _ => EditorOptions.TutorialTaskTypes(), describe: Describe(FieldHelp.TutorialTasks)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Stat"), _ => EditorOptions.Statistics()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Window"), _ => EditorOptions.Windows()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("What"), _ => EditorOptions.ToggleTargets()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Machine"), e => e.Assets.RecipeTypes));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Slot"), _ => EquipmentSlots));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && (c.NameIs("RequiresGameMode") || c.PathContains("GameModes")), _ => EditorOptions.GameModeNames));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Icon"), e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Action"), _ => InputActions()));
        widgets.Add(new ReferenceFieldWidget(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Block"), e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));

        widgets.AddKeySuggestions(c => c.Scope == EditorScopes.Biomes && c.NameIs("Asteroids"), e => e.Assets.Asteroids);
        widgets.AddKeySuggestions(c => c.NameIs("BiomeMultipliers"), e => e.Assets.Biomes);
        return widgets;
    }

    private static readonly string[] GameModeBases = { "survival", "creative", "spectator" };

    private static Func<string, string> Describe(string group) => key => FieldHelp.Default.Text(group, key);

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
        if (context.NameIs("Item", "Drop", "Ammo", "DnaItem", "WaterItem")) return true;
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
        if (context.Name.EndsWith("Sound", StringComparison.OrdinalIgnoreCase)) return true;
        return context.PathContains("Sounds");
    }
}
