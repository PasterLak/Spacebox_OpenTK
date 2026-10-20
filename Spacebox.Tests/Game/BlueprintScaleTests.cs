using Spacebox.Game;

namespace Spacebox.Tests;

public class BlueprintScaleTests
{
    private static readonly Item Ore = new Item(101, 64, "ore") { Id_string = "test:ore" };
    private static readonly Item Ingot = new Item(102, 64, "ingot") { Id_string = "test:ingot" };

    private static Blueprint Create(byte ingredient, byte product)
    {
        return new Blueprint
        {
            Ingredients = new[] { new Ingredient(Ore, ingredient) },
            Product = new Product(Ingot, product)
        };
    }

    [Fact]
    public void ScaleBlueprint_MultipliesSmallQuantities()
    {
        var blueprint = Create(4, 2);

        Blueprint.ScaleBlueprint(blueprint, 5);

        Assert.Equal(20, blueprint.Ingredients[0].Quantity);
        Assert.Equal(10, blueprint.Product.Quantity);
    }

    [Fact]
    public void ScaleBlueprint_NeverOverflowsByteQuantities()
    {
        var blueprint = Create(52, 1);

        Blueprint.ScaleBlueprint(blueprint, 5);

        Assert.Equal(208, blueprint.Ingredients[0].Quantity);
        Assert.Equal(4, blueprint.Product.Quantity);
    }
}
