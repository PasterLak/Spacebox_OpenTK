using System.Text.Json.Nodes;
using Spacebox.Editor.Canvas.Inspector;
using Spacebox.Editor.Canvas.Pages;
using Spacebox.Editor.Canvas.Preview;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas;

public static class EditorPages
{
    public static List<EditorPage> CreateDefault(GameSetProject project)
    {
        var widgets = FieldWidgetRegistry.CreateDefault();
        var itemIcon = new ItemIdIconProvider(ItemIdIconProvider.TopLevelId);
        var productIcon = new ItemIdIconProvider(ItemIdIconProvider.ProductId);

        return new List<EditorPage>
        {
            new OverviewPage(widgets),
            new JsonFilePage("Player spawn", Spacebox.Game.Player.Respawn.SpawnParameters.RelativePath, EditorScopes.PlayerSpawn, widgets,
                "Start items and where they go, start health/power/oxygen, and which asteroid new players start next to. Used for new players and respawns."),
            new CollectionPage("Blocks", EditorScopes.Blocks, GameSetSources.Blocks(project), widgets, new BlockPreviewView(), itemIcon,
                description: "Every block of the GameSet: textures, model, behaviour type and its settings."),
            new CollectionPage("Items", EditorScopes.Items, GameSetSources.Items(project), widgets, new ItemPreviewView(), new SpriteIconProvider(),
                description: "Every item: tools, weapons, consumables, equipment and the items of blocks."),
            new CollectionPage("Projectiles", EditorScopes.Projectiles,
                new FolderEntrySource(project, GameSetCatalog.ProjectilesFolder, GameSetCatalog.Projectiles), widgets, new ProjectilePreviewView(),
                description: "Bullets of weapons and cannons."),
            new TabbedPage("Recipes", "Crafting blueprints, machine recipes and crafting window categories.",
                new CollectionPage("Crafting", EditorScopes.Crafting, GameSetSources.Blueprints(project), widgets, null, productIcon, new RecipeSummaryView()),
                new CollectionPage("Processing", EditorScopes.Recipes, GameSetSources.ProcessingRecipes(project), widgets, null, productIcon, new RecipeSummaryView()),
                new CollectionPage("Categories", EditorScopes.Crafting, GameSetSources.Categories(project), widgets, null,
                    new ItemIdIconProvider(node => Field(node, "Icon")), new CraftingCategoryView())),
            new TabbedPage("Disassembly", "What the disassembler gives back for each item.",
                new CollectionPage("Items", EditorScopes.Disassembly,
                    new ArrayEntrySource(project, GameSetCatalog.DisassemblyFile, "Items", GameSetCatalog.DisassemblyItems), widgets, null,
                    new ItemIdIconProvider(node => Field(node, "Item")), new DisassemblySummaryView()),
                new JsonFilePage("Defaults", GameSetCatalog.DisassemblyFile, EditorScopes.Disassembly, widgets,
                    "Defaults for every item without its own entry. Items not listed are disassembled from their crafting or machine recipe.")),
            new CollectionPage("Loot", EditorScopes.Loot,
                new FolderEntrySource(project, GameSetCatalog.LootFolder, GameSetCatalog.Loot), widgets,
                description: "Loot tables of storages found in the world."),
            new CollectionPage("Mobs", EditorScopes.Mobs,
                new FolderEntrySource(project, GameSetCatalog.MobsFolder, GameSetCatalog.Mobs), widgets, new MobPreviewView(),
                description: "Creatures: model, behaviour, stats and where they spawn."),
            new TabbedPage("Generation", "Asteroids, biomes and the world generator.",
                new CollectionPage("Asteroids", EditorScopes.Asteroids,
                    new FolderEntrySource(project, GameSetCatalog.AsteroidsFolder, GameSetCatalog.Asteroids), widgets, new AsteroidPreviewView()),
                new BiomeMapPage(),
                new CollectionPage("Biomes", EditorScopes.Biomes,
                    new FolderEntrySource(project, GameSetCatalog.BiomesFolder, GameSetCatalog.Biomes), widgets),
                new JsonFilePage("Generator settings", GameSetCatalog.GeneratorFile, EditorScopes.Generator, widgets,
                    "Generation changes apply to the previews after Save and Reload.")),
            new CollectionPage("Tutorials", EditorScopes.Tutorials,
                new FolderEntrySource(project, GameSetCatalog.TutorialsFolder, GameSetCatalog.Tutorials), widgets, new TutorialPreviewView(),
                description: "Tutorials that teach new players the game, checked with the game's own parser."),
            new SoundsPage(),
            new AtlasPage(),
            new FilesPage(widgets)
        };
    }

    private static string Field(JsonNode node, string name) => node is JsonObject obj ? EditorJson.GetString(obj, name) : "";
}
