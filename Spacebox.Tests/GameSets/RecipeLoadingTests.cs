using System.Text.Json.Nodes;
using Spacebox.Game;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

[Collection("GameAssets")]
public sealed class RecipeLoadingTests : IDisposable
{
    private const string Type = "recipetest_smelter";

    private readonly Item _ore = new Item(971, 64, "Test ore") { Id_string = "recipetest:ore" };
    private readonly Item _ingot = new Item(972, 64, "Test ingot") { Id_string = "recipetest:ingot" };
    private readonly Item _rock = new Item(973, 64, "Test rock") { Id_string = "recipetest:rock" };

    public RecipeLoadingTests()
    {
        foreach (var item in new[] { _ore, _ingot, _rock })
        {
            GameAssets.Items[item.Id] = item;
            GameAssets.ItemsStr[item.Id_string] = item;
        }
    }

    public void Dispose()
    {
        foreach (var item in new[] { _ore, _ingot, _rock })
        {
            GameAssets.Items.Remove(item.Id);
            GameAssets.ItemsStr.Remove(item.Id_string);
        }
        GameAssets.Recipes.Remove(Type);
    }

    [Fact]
    public void LoadRecipes_SkipsABrokenRecipeAndKeepsTheRest()
    {
        var recipes = new List<ContentEntry>
        {
            Entry("{ \"Type\": \"" + Type + "\", \"Ingredient\": { \"Item\": \"recipetest:rock\", \"Quantity\": 1 }, \"Product\": { \"Item\": \"recipetest:ingot\", \"Quantity\": 1 } }"),
            Entry("{ \"Type\": \"" + Type + "\", \"Ingredient\": \"broken\", \"Product\": { \"Item\": \"recipetest:ingot\", \"Quantity\": 1 } }"),
            Entry("{ \"Type\": \"" + Type + "\", \"Ingredient\": { \"Item\": \"recipetest:ore\", \"Quantity\": 1 }, \"Product\": { \"Item\": \"recipetest:ingot\", \"Quantity\": 1 } }")
        };
        var content = new GameSetContent(Array.Empty<ContentLayer>(), Array.Empty<ContentEntry>(), Array.Empty<ContentEntry>(), Array.Empty<ContentEntry>(), Array.Empty<ContentEntry>(), recipes, Array.Empty<string>());

        GameSetLoader.LoadRecipes(content);

        Assert.True(GameAssets.Recipes[Type].ContainsKey(_rock.Id));
        Assert.True(GameAssets.Recipes[Type].ContainsKey(_ore.Id));
    }

    private static ContentEntry Entry(string json)
    {
        return new ContentEntry("", (JsonObject)JsonNode.Parse(json)!, null, "recipes.json", null);
    }
}
