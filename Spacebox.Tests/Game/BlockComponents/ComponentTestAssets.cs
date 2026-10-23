using Spacebox.Game;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game.BlockComponents;

public sealed class ComponentTestAssets : IDisposable
{
    public const string RecipeType = "comptest_smelter";

    public readonly Item Ore = new Item(951, 64, "Test ore") { Id_string = "comptest:ore" };
    public readonly Item Ingot = new Item(952, 64, "Test ingot") { Id_string = "comptest:ingot" };
    public readonly Item Rock = new Item(953, 64, "Test rock") { Id_string = "comptest:rock" };

    public ComponentTestAssets(short recipeTicks = 4)
    {
        Register(Ore);
        Register(Ingot);
        Register(Rock);

        var recipe = new Recipe { RequiredTicks = recipeTicks, Ingredient = new Ingredient(Ore, 1), Product = new Product(Ingot, 1) };
        GameAssets.Recipes[RecipeType] = new Dictionary<short, Recipe> { { Ore.Id, recipe } };
    }

    public static BlockData Data(string type, string name = "Test machine") => new BlockData(name, type) { Id = 900, Id_string = "comptest:" + type, Durability = 10 };

    public static ComponentBlock Modular(params BlockComponentDefinition[] definitions) => ComponentBlocks.Create(Data(BlockComponentRegistry.ModularType), definitions);

    public static ProcessingComponentDefinition Processing(ushort power = 0) => new ProcessingComponentDefinition(RecipeType, power, 1f);

    public void Dispose()
    {
        Unregister(Ore);
        Unregister(Ingot);
        Unregister(Rock);
        GameAssets.Recipes.Remove(RecipeType);
    }

    private static void Register(Item item)
    {
        GameAssets.Items[item.Id] = item;
        GameAssets.ItemsStr[item.Id_string] = item;
    }

    private static void Unregister(Item item)
    {
        GameAssets.Items.Remove(item.Id);
        GameAssets.ItemsStr.Remove(item.Id_string);
    }
}
