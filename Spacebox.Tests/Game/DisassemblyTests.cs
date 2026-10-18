using Spacebox.Game;
using Spacebox.Game.Disassembly;

namespace Spacebox.Tests;

public class DisassemblyTests
{
    private const int DefaultTicks = 200;

    private static readonly Item Ingot = CreateItem(1, "ingot");
    private static readonly Item Panel = CreateItem(2, "panel");
    private static readonly Item Cell = CreateItem(3, "cell");
    private static readonly Item Drill = CreateItem(4, "drill", 1);
    private static readonly Item Block = CreateItem(5, "block");
    private static readonly Item Dust = CreateItem(6, "dust");
    private static readonly Item Health = CreateItem(7, "$health");
    private static readonly Item Loot = CreateItem(8, "loot");

    private static Item CreateItem(short id, string name, byte stack = 64)
    {
        return new Item(id, stack, name) { Id_string = name.StartsWith('$') ? name : "test:" + name };
    }

    private static DisassemblySource Source(byte productQuantity, int? ticks, params (Item item, byte quantity)[] ingredients)
    {
        return new DisassemblySource(ingredients.Select(i => new Ingredient(i.item, i.quantity)).ToArray(), productQuantity, ticks);
    }

    private static int QuantityOf(DisassemblyRecipe recipe, Item item)
    {
        return recipe.Products.Where(p => p.Item.Id == item.Id).Sum(p => p.Quantity);
    }

    [Fact]
    public void FromSource_ReturnsHalfRoundedDown()
    {
        var source = Source(1, null, (Ingot, 4), (Cell, 1));

        var recipe = DisassemblyCalculator.FromSource(Drill, source, 50, null, DefaultTicks);

        Assert.NotNull(recipe);
        Assert.Equal(2, QuantityOf(recipe, Ingot));
        Assert.Equal(0, QuantityOf(recipe, Cell));
        Assert.Single(recipe.Products);
    }

    [Fact]
    public void FromSource_SingleIngredientOfOne_GivesNothing()
    {
        var source = Source(1, null, (Ingot, 1));

        Assert.Null(DisassemblyCalculator.FromSource(Drill, source, 50, null, DefaultTicks));
    }

    [Fact]
    public void FromSource_UsesCraftedBatchAsInput()
    {
        var source = Source(3, null, (Ingot, 1));

        var recipe = DisassemblyCalculator.FromSource(Panel, source, 100, null, DefaultTicks);

        Assert.NotNull(recipe);
        Assert.Equal(3, recipe.InputQuantity);
        Assert.Equal(1, QuantityOf(recipe, Ingot));
    }

    [Fact]
    public void FromSource_SkipsVirtualResourcesAndClampsPercent()
    {
        var source = Source(1, null, (Ingot, 2), (Health, 15));

        var recipe = DisassemblyCalculator.FromSource(Drill, source, 250, null, DefaultTicks);

        Assert.NotNull(recipe);
        Assert.Equal(2, QuantityOf(recipe, Ingot));
        Assert.Equal(0, QuantityOf(recipe, Health));
    }

    [Fact]
    public void Cheapest_PicksLowestReturnAndItsDuration()
    {
        var expensive = Source(1, null, (Ingot, 8));
        var cheap = Source(1, 100, (Ingot, 2));

        var recipe = DisassemblyCalculator.Cheapest(Dust, [expensive, cheap], 50, null, null, DefaultTicks);

        Assert.NotNull(recipe);
        Assert.Equal(1, QuantityOf(recipe, Ingot));
        Assert.Equal(100, recipe.DurationTicks);
    }

    [Fact]
    public void Cheapest_AnyFreeSource_MakesItemNotDisassemblable()
    {
        var expensive = Source(1, null, (Panel, 3));
        var free = Source(1, 100, (Ingot, 1));

        Assert.Null(DisassemblyCalculator.Cheapest(Dust, [expensive, free], 50, null, null, DefaultTicks));
    }

    [Fact]
    public void Resolver_UsesDefaultDurationForCrafting()
    {
        var resolver = CreateResolver(new DisassemblySettings(50, DefaultTicks, []), Source(1, null, (Ingot, 4)));

        Assert.True(resolver.TryGet(Drill, out var recipe));
        Assert.Equal(DefaultTicks, recipe.DurationTicks);
        Assert.Equal(2, QuantityOf(recipe, Ingot));
    }

    [Fact]
    public void Resolver_AppliesPercentAndDurationOverrides()
    {
        var entry = new DisassemblyEntry { Item = Drill, ReturnPercent = 100, DurationTicks = 40 };
        var resolver = CreateResolver(new DisassemblySettings(50, DefaultTicks, [entry]), Source(1, null, (Ingot, 4)));

        Assert.True(resolver.TryGet(Drill, out var recipe));
        Assert.Equal(4, QuantityOf(recipe, Ingot));
        Assert.Equal(40, recipe.DurationTicks);
    }

    [Fact]
    public void Resolver_ExplicitProductsWinOverCrafting()
    {
        var entry = new DisassemblyEntry { Item = Drill, Products = [new Product(Cell, 3)] };
        var resolver = CreateResolver(new DisassemblySettings(50, DefaultTicks, [entry]), Source(1, null, (Ingot, 4)));

        Assert.True(resolver.TryGet(Drill, out var recipe));
        Assert.Equal(3, QuantityOf(recipe, Cell));
        Assert.Equal(0, QuantityOf(recipe, Ingot));
    }

    [Fact]
    public void Resolver_DisabledEntryBlocksDisassembly()
    {
        var entry = new DisassemblyEntry { Item = Drill, Disabled = true };
        var resolver = CreateResolver(new DisassemblySettings(50, DefaultTicks, [entry]), Source(1, null, (Ingot, 4)));

        Assert.False(resolver.TryGet(Drill, out _));
    }

    [Fact]
    public void Resolver_ItemWithoutRecipe_IsNotDisassemblable()
    {
        var resolver = CreateResolver(new DisassemblySettings(50, DefaultTicks, []), Source(1, null, (Ingot, 4)));

        Assert.False(resolver.TryGet(Loot, out _));
    }

    [Fact]
    public void Audit_FlagsExplicitRecipeGivingMoreThanCraftingCosts()
    {
        var fair = new DisassemblyEntry { Item = Drill, Products = [new Product(Ingot, 4)] };
        var cheat = new DisassemblyEntry { Item = Block, Products = [new Product(Ingot, 5)] };
        var settings = new DisassemblySettings(50, DefaultTicks, [fair, cheat]);
        var provider = new FakeProvider { [Drill.Id] = Source(1, null, (Ingot, 4)), [Block.Id] = Source(1, null, (Ingot, 4)) };

        var issues = DisassemblyBalanceAudit.FindIssues(settings, [provider]);

        Assert.Single(issues);
        Assert.Contains("block", issues[0]);
    }

    [Fact]
    public void Inventory_ProcessesWhenEnoughInputAndSpace()
    {
        var recipe = new DisassemblyRecipe(Block, 2, [new Product(Panel, 3)], DefaultTicks);
        var inventory = new DisassemblerInventory(Lookup(recipe));
        inventory.Input.SetSlot(0, 0, Block, 1);
        inventory.Input.SetSlot(1, 0, Block, 2);

        Assert.Equal(DisassemblerStatus.Working, inventory.FindRecipe(out var found));
        Assert.Same(recipe, found);
        Assert.True(inventory.TryProcess(recipe));

        Assert.Equal(1, inventory.CountInput(Block));
        Assert.Equal(3, inventory.Output.GetTotalCountOf(Panel));
    }

    [Fact]
    public void Inventory_ReportsNotEnoughItems()
    {
        var recipe = new DisassemblyRecipe(Block, 4, [new Product(Panel, 1)], DefaultTicks);
        var inventory = new DisassemblerInventory(Lookup(recipe));
        inventory.Input.SetSlot(0, 0, Block, 3);

        Assert.Equal(DisassemblerStatus.NotEnoughItems, inventory.FindRecipe(out var found));
        Assert.Null(found);
    }

    [Fact]
    public void Inventory_ReportsOutputFullAndKeepsInput()
    {
        var recipe = new DisassemblyRecipe(Block, 1, [new Product(Panel, 2)], DefaultTicks);
        var inventory = new DisassemblerInventory(Lookup(recipe));
        inventory.Input.SetSlot(0, 0, Block, 1);

        for (byte y = 0; y < DisassemblerInventory.Height; y++)
        for (byte x = 0; x < DisassemblerInventory.Width; x++)
            inventory.Output.SetSlot(x, y, Panel, x == 0 && y == 0 ? (byte)(Panel.StackSize - 1) : Panel.StackSize);

        Assert.Equal(DisassemblerStatus.OutputFull, inventory.FindRecipe(out _));
        Assert.False(inventory.TryProcess(recipe));
        Assert.Equal(1, inventory.CountInput(Block));
    }

    [Fact]
    public void Inventory_SplitsProductsAcrossStacks()
    {
        var recipe = new DisassemblyRecipe(Block, 1, [new Product(Panel, 10)], DefaultTicks);
        var inventory = new DisassemblerInventory(Lookup(recipe));
        inventory.Input.SetSlot(0, 0, Block, 1);
        inventory.Output.SetSlot(0, 0, Panel, (byte)(Panel.StackSize - 4));

        Assert.True(inventory.TryProcess(recipe));

        Assert.Equal(Panel.StackSize, inventory.Output.GetSlot(0, 0).Count);
        Assert.Equal(6, inventory.Output.GetSlot(1, 0).Count);
    }

    [Fact]
    public void Inventory_OutputSlotsRejectPlayerItems()
    {
        var inventory = new DisassemblerInventory(Lookup(new DisassemblyRecipe(Block, 1, [new Product(Panel, 1)], DefaultTicks)));

        Assert.False(inventory.Output.GetSlot(0, 0).CanAccept(Panel));
        Assert.True(inventory.Input.GetSlot(0, 0).CanAccept(Block));
        Assert.False(inventory.Input.GetSlot(0, 0).CanAccept(Loot));
    }

    private static DisassemblyRecipeResolver CreateResolver(DisassemblySettings settings, DisassemblySource drillSource)
    {
        var provider = new FakeProvider { [Drill.Id] = drillSource };
        return new DisassemblyRecipeResolver(
        [
            new ConfiguredDisassemblyRule(settings),
            new DerivedDisassemblyRule(settings, [provider])
        ]);
    }

    private static DisassemblyRecipeLookup Lookup(DisassemblyRecipe recipe)
    {
        return (Item item, out DisassemblyRecipe found) =>
        {
            found = item.Id == recipe.Input.Id ? recipe : null!;
            return found != null;
        };
    }

    private sealed class FakeProvider : Dictionary<short, DisassemblySource>, IDisassemblySourceProvider
    {
        public void Collect(Item item, List<DisassemblySource> sources)
        {
            if (TryGetValue(item.Id, out var source)) sources.Add(source);
        }
    }
}
