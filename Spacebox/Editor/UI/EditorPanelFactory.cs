using Spacebox.Editor.Data;
using Spacebox.Editor.UI.Fields;
using Spacebox.Editor.UI.Panels;

namespace Spacebox.Editor.UI;

public static class EditorPanelFactory
{
    public static List<IEditorPanel> CreateDefault(GameSetProject project)
    {
        var inspector = new JsonInspector(JsonFieldEditors.CreateDefault());
        var itemIcon = new ItemIdIconProvider(ItemIdIconProvider.TopLevelId);
        var productIcon = new ItemIdIconProvider(ItemIdIconProvider.ProductId);
        var recipeSummary = new RecipeSummary();

        var asteroids = new CollectionPanel("Asteroids", EditorScopes.Asteroids,
            new FolderEntrySource(project, GameSetCatalog.AsteroidsFolder, GameSetCatalog.Asteroids), inspector, new AsteroidEntryPreview());
        var biomes = new CollectionPanel("Biomes", EditorScopes.Biomes,
            new FolderEntrySource(project, GameSetCatalog.BiomesFolder, GameSetCatalog.Biomes), inspector);

        return new List<IEditorPanel>
        {
            new OverviewPanel(inspector),
            new CollectionPanel("Blocks", EditorScopes.Blocks,
                GameSetSources.Blocks(project), inspector, new BlockEntryPreview(), itemIcon),
            new CollectionPanel("Items", EditorScopes.Items,
                GameSetSources.Items(project), inspector, new ItemEntryPreview(), new SpriteIconProvider()),
            new CollectionPanel("Projectiles", EditorScopes.Projectiles,
                new FolderEntrySource(project, GameSetCatalog.ProjectilesFolder, GameSetCatalog.Projectiles), inspector, new ProjectileEntryPreview()),
            new TabbedPanel("Recipes",
                new CollectionPanel("Crafting", EditorScopes.Crafting,
                    GameSetSources.Blueprints(project), inspector, null, productIcon, recipeSummary),
                new CollectionPanel("Processing", EditorScopes.Recipes,
                    GameSetSources.ProcessingRecipes(project), inspector, null, productIcon, recipeSummary),
                new CollectionPanel("Categories", EditorScopes.Crafting,
                    GameSetSources.Categories(project), inspector, null,
                    new ItemIdIconProvider(node => node is System.Text.Json.Nodes.JsonObject obj ? EditorJson.GetString(obj, "Icon") : ""))),
            new CollectionPanel("Loot", EditorScopes.Loot,
                new FolderEntrySource(project, GameSetCatalog.LootFolder, GameSetCatalog.Loot), inspector),
            new CollectionPanel("Mobs", EditorScopes.Mobs,
                new FolderEntrySource(project, GameSetCatalog.MobsFolder, GameSetCatalog.Mobs), inspector, new MobEntryPreview()),
            new GenerationPanel(inspector, asteroids, biomes),
            new AtlasPanel(),
            new FilesPanel(inspector)
        };
    }
}
