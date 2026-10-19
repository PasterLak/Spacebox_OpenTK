using System.Text.Json;
using Xunit;

namespace Spacebox.Tests.Game;

public class DefaultCrusherRecipesTests
{
    private static string GameSetFile(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", "GameSets", "Default", name);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(name);
    }

    private static JsonElement Load(string name) => JsonDocument.Parse(File.ReadAllText(GameSetFile(name))).RootElement;

    private static IEnumerable<JsonElement> Walk(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
                foreach (var child in Walk(property.Value)) yield return child;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                foreach (var child in Walk(item)) yield return child;
        }
    }

    private static HashSet<string> Ids()
    {
        var ids = new HashSet<string>();
        foreach (var file in new[] { "items.json", "blocks.json" })
            foreach (var element in Walk(Load(file)))
                if (element.TryGetProperty("ID", out var id)) ids.Add(id.GetString());
        return ids;
    }

    private static List<(string In, int InQty, string Out, int OutQty)> CrusherRecipes()
    {
        var recipes = new List<(string, int, string, int)>();
        foreach (var category in Load("recipes.json").GetProperty("RecipeCatalog").EnumerateArray())
        {
            if (category.GetProperty("Type").GetString() != "crusher") continue;

            foreach (var recipe in category.GetProperty("Recipes").EnumerateArray())
            {
                var ingredient = recipe.GetProperty("Ingredient");
                var product = recipe.GetProperty("Product");
                recipes.Add((ingredient.GetProperty("Item").GetString(), ingredient.GetProperty("Quantity").GetInt32(),
                    product.GetProperty("Item").GetString(), product.GetProperty("Quantity").GetInt32()));
            }
        }
        return recipes;
    }

    private static Dictionary<string, (int Quantity, List<(string Item, int Quantity)> Ingredients)> Crafting()
    {
        var crafting = new Dictionary<string, (int, List<(string, int)>)>();
        foreach (var element in Walk(Load("crafting.json")))
        {
            if (!element.TryGetProperty("Product", out var product) || !element.TryGetProperty("Ingredients", out var ingredients)) continue;
            if (product.ValueKind != JsonValueKind.Object) continue;

            var list = new List<(string, int)>();
            foreach (var ingredient in ingredients.EnumerateArray())
                list.Add((ingredient.GetProperty("Item").GetString(), ingredient.GetProperty("Quantity").GetInt32()));
            crafting[product.GetProperty("Item").GetString()] = (product.GetProperty("Quantity").GetInt32(), list);
        }
        return crafting;
    }

    private static double? IngotValue(string item, string metal, Dictionary<string, (int Quantity, List<(string Item, int Quantity)> Ingredients)> crafting, int depth = 0)
    {
        if (item == metal + "_ingot") return 1;
        if (depth > 6 || !crafting.TryGetValue(item, out var recipe)) return null;

        double total = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            var value = IngotValue(ingredient.Item, metal, crafting, depth + 1);
            if (value != null) total += value.Value * ingredient.Quantity;
        }
        return total > 0 ? total / recipe.Quantity : null;
    }

    [Fact]
    public void CrusherRecipes_UseExistingItemsOnce()
    {
        var ids = Ids();
        var ingredients = new HashSet<string>();

        foreach (var recipe in CrusherRecipes())
        {
            Assert.Contains(recipe.In, ids);
            Assert.Contains(recipe.Out, ids);
            Assert.True(ingredients.Add(recipe.In), $"{recipe.In} has two crusher recipes");
            Assert.True(recipe.InQty > 0 && recipe.OutQty > 0);
        }
    }

    [Fact]
    public void CrusherRecycling_NeverReturnsMoreMetalThanCrafted()
    {
        var crafting = Crafting();

        foreach (var recipe in CrusherRecipes())
        {
            if (!recipe.Out.EndsWith("_dust")) continue;

            string metal = recipe.Out.Substring(0, recipe.Out.Length - "_dust".Length);
            var value = IngotValue(recipe.In, metal, crafting);
            if (value == null) continue;

            Assert.True(recipe.OutQty <= value.Value * recipe.InQty + 1e-6,
                $"{recipe.In} x{recipe.InQty} gives {recipe.OutQty} {recipe.Out}, but contains only {value * recipe.InQty:0.##} ingots");
        }
    }
}
