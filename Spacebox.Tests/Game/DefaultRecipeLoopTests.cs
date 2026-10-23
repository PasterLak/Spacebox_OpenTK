using System.Text.Json.Nodes;

namespace Spacebox.Tests.Game;

public class DefaultRecipeLoopTests
{
    private readonly record struct Conversion(string Input, float InputCount, string Output, float OutputCount);

    [Fact]
    public void ProcessingNeverReturnsMoreOfAMaterialThanTheBlueprintUsed()
    {
        var processing = new List<Conversion>();
        foreach (var group in DefaultGameSetContent.Content.RecipesJson()["RecipeCatalog"]!.AsArray())
            foreach (var recipe in group!["Recipes"]!.AsArray())
                processing.Add(new Conversion(Item(recipe!["Ingredient"]), Count(recipe["Ingredient"]), Item(recipe["Product"]), Count(recipe["Product"])));

        var problems = new List<string>();
        foreach (var entry in DefaultGameSetContent.Content.Blueprints)
        {
            var blueprint = entry.Data;
            string product = Item(blueprint["Product"]);
            float made = Count(blueprint["Product"]);
            if (product == null || made <= 0f || blueprint["Ingredients"] is not JsonArray ingredients) continue;

            foreach (var back in processing)
            {
                if (back.Input != product) continue;

                float returned = back.OutputCount / back.InputCount;
                float spent = 0f;
                foreach (var ingredient in ingredients)
                    spent += Count(ingredient) / made * MaterialPerUnit(processing, back.Output, Item(ingredient));

                if (spent > 0f && returned > spent + 0.001f)
                    problems.Add($"{product} -> {returned} {back.Output}, but costs only {spent} {back.Output}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static float MaterialPerUnit(List<Conversion> processing, string material, string item)
    {
        if (item == material) return 1f;
        foreach (var conversion in processing)
            if (conversion.Input == material && conversion.Output == item) return conversion.InputCount / conversion.OutputCount;
        return 0f;
    }

    private static string Item(JsonNode node)
    {
        string id = node?["Item"]?.GetValue<string>();
        if (id == null) return null;
        int colon = id.IndexOf(':');
        return colon >= 0 ? id[(colon + 1)..] : id;
    }

    private static float Count(JsonNode node) => node?["Quantity"]?.GetValue<float>() ?? 1f;
}
