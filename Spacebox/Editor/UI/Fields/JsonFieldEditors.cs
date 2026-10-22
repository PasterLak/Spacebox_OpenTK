using System.Text.Json.Nodes;
using Spacebox.Editor.Data;
using Spacebox.Game;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.UI.Fields;

public sealed class JsonFieldEditors
{
    private static readonly string[] EquipmentSlots = Enum.GetNames(typeof(EquipmentSlotType));

    private readonly List<IJsonFieldEditor> _editors = new List<IJsonFieldEditor>();
    private readonly List<(Func<FieldContext, bool> Matches, Func<EditorContext, IReadOnlyList<string>> Keys)> _keySuggestions = new();
    private readonly IJsonFieldEditor _fallback = new PrimitiveFieldEditor();

    public static JsonFieldEditors CreateDefault()
    {
        var editors = new JsonFieldEditors();
        editors.Add(new ColorFieldEditor());
        editors.Add(new VectorFieldEditor());
        editors.Add(new NumberRangeFieldEditor(c => c.Scope == EditorScopes.Asteroids && c.NameIs("MinRadius", "MaxRadius"), AsteroidRadius.Min, AsteroidRadius.Max));

        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Blocks && c.PathContains("Textures"), e => e.Assets.BlockTextures, (e, n) => e.Assets.BlockTexture(n)));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("Sprite"), e => e.Assets.ItemSprites, (e, n) => e.Assets.ItemSprite(n)));
        editors.Add(new ReferenceFieldEditor(IsItemReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        editors.Add(new ReferenceFieldEditor(IsBlockReference, e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("Projectile"), e => e.Assets.Projectiles));
        editors.Add(new ReferenceFieldEditor(IsSoundReference, e => e.Assets.Sounds));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Blocks && c.NameIs("Type") && IsTopLevel(c), _ => GameSetCatalog.BlockTypeNames.OrderBy(t => t).ToArray()));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Items && c.NameIs("Type") && IsTopLevel(c), _ => GameSetCatalog.ItemTypeNames.ToArray()));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("Category") && IsTopLevel(c), e => e.Assets.Categories));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Items && c.NameIs("Slot"), _ => EquipmentSlots));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("Mob"), e => e.Assets.Mobs));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("Loot"), e => e.Assets.LootTables));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Generator && c.PathContains("Biomes"), e => e.Assets.Biomes));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("FallbackBiome", "PreferredSpawnBiome") || c.Scope == EditorScopes.Asteroids && c.PathContains("Mobs") && c.PathContains("Biomes"), e => e.Assets.Biomes));
        editors.Add(new ReferenceFieldEditor(c => c.PathContains("ExcludedAsteroids"), e => e.Assets.Asteroids));
        editors.Add(new ReferenceFieldEditor(c => c.NameIs("PutIn"), _ => Spacebox.Game.Player.StartItemPlacement.All));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Type"), _ => Spacebox.Game.Tutorial.Tasks.TutorialTaskFactory.Types.OrderBy(t => t).ToArray()));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Action"), _ => TutorialActions()));
        editors.Add(new ReferenceFieldEditor(c => c.Scope == EditorScopes.Tutorials && c.NameIs("Block"), e => e.Assets.ItemIds, (e, n) => e.Assets.ItemIcon(n)));

        editors.AddKeySuggestions(c => c.Scope == EditorScopes.Biomes && c.NameIs("Asteroids"), e => e.Assets.Asteroids);
        editors.AddKeySuggestions(c => c.NameIs("BiomeMultipliers"), e => e.Assets.Biomes);
        return editors;
    }

    public void Add(IJsonFieldEditor editor)
    {
        _editors.Add(editor);
    }

    public void AddKeySuggestions(Func<FieldContext, bool> matches, Func<EditorContext, IReadOnlyList<string>> keys)
    {
        _keySuggestions.Add((matches, keys));
    }

    public IReadOnlyList<string> GetKeySuggestions(FieldContext context)
    {
        for (int i = 0; i < _keySuggestions.Count; i++)
        {
            if (_keySuggestions[i].Matches(context)) return _keySuggestions[i].Keys(context.Editor);
        }
        return Array.Empty<string>();
    }

    public bool CanDrawInline(FieldContext context)
    {
        if (context.Value is JsonValue || context.Value == null) return true;
        return Find(context) != null;
    }

    public bool Draw(FieldContext context, out JsonNode value)
    {
        var editor = Find(context) ?? _fallback;
        return editor.Draw(context, out value);
    }

    private IJsonFieldEditor Find(FieldContext context)
    {
        for (int i = 0; i < _editors.Count; i++)
        {
            if (_editors[i].CanEdit(context)) return _editors[i];
        }
        return null;
    }

    private static string[] TutorialActions()
    {
        var actions = Engine.InputPro.InputManager.Instance?.GetAllActions();
        if (actions == null) return Array.Empty<string>();

        var names = new List<string>(actions.Count);
        foreach (var (id, _) in actions) names.Add(id);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names.ToArray();
    }

    private static bool IsTopLevel(FieldContext context)
    {
        return context.Path.IndexOf('.') < 0;
    }

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
