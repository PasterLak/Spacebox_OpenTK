using System.Text.Json.Nodes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Tests.GameSets;

public sealed class PackNamespaceTests : IDisposable
{
    private readonly TempGameSets _sets = new TempGameSets();

    public void Dispose() => _sets.Dispose();

    private GameSetContent Read(params string[] extensions)
    {
        var result = GameSetStackResolver.Resolve(ModFolders.Flat(_sets.Root), "default", extensions);
        Assert.True(result.Success, result.ErrorText);
        return result.Stack.ReadContent();
    }

    private void WriteBase()
    {
        _sets.Config("Empty", "empty");
        _sets.Config("Default", "default", "empty");
        _sets.Write("Default/Blocks/stone/stone.json", "{ \"ID\": \"stone\", \"Name\": \"Stone\" }");
        _sets.Write("Default/Items/iron_ingot/iron_ingot.json", "{ \"ID\": \"iron_ingot\", \"Name\": \"Iron\" }");
    }

    private static string Item(ContentEntry entry, string field) => (string)((JsonObject)entry.Data[field]!)["Item"]!;

    [Fact]
    public void NewPackEntries_GetThePackNamespace()
    {
        WriteBase();
        _sets.Config("Weapons", "weapons", "*");
        _sets.Write("Weapons/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Laser\" }");

        var content = Read("weapons");

        var laser = Assert.Single(content.Items, i => i.Key.EndsWith("laser"));
        Assert.Equal("weapons:laser", laser.Key);
        Assert.Equal("weapons", laser.Namespace);
    }

    [Fact]
    public void TwoPacksWithTheSameId_DoNotClash()
    {
        WriteBase();
        _sets.Config("Weapons", "weapons", "*");
        _sets.Write("Weapons/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Laser A\" }");
        _sets.Config("Guns", "guns", "*");
        _sets.Write("Guns/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Laser B\" }");

        var content = Read("weapons", "guns");

        Assert.Equal(new[] { "guns:laser", "weapons:laser" }, content.Items.Where(i => i.Key.EndsWith(":laser")).Select(i => i.Key).OrderBy(k => k).ToArray());
        Assert.Empty(content.Warnings);
    }

    [Fact]
    public void PacksPatchBaseEntriesByTheirShortId()
    {
        WriteBase();
        _sets.Config("Weapons", "weapons", "*");
        _sets.Write("Weapons/Blocks/stone/stone.json", "{ \"ID\": \"stone\", \"Name\": \"Hard Stone\" }");
        _sets.Write("Weapons/Blocks/marble/marble.json", "{ \"ID\": \"default:marble\", \"Name\": \"Marble\" }");

        var content = Read("weapons");

        var stone = Assert.Single(content.Blocks, b => b.Key == "stone");
        Assert.Equal("Hard Stone", (string)stone.Data["Name"]!);
        Assert.Null(stone.Namespace);
        Assert.Contains(content.Blocks, b => b.Key == "marble");
    }

    [Fact]
    public void PackRecipes_PointToThePackOwnItemsFirstThenTheBase()
    {
        WriteBase();
        _sets.Config("Weapons", "weapons", "*");
        _sets.Write("Weapons/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Laser\" }");
        _sets.Write("Weapons/Items/laser/blueprint.json", "{ \"Product\": { \"Item\": \"laser\" }, \"Ingredients\": [ { \"Item\": \"iron_ingot\", \"Quantity\": 2 } ] }");

        var blueprint = Assert.Single(Read("weapons").Blueprints, b => b.Key.Contains("laser"));

        Assert.Equal("weapons:laser", blueprint.Key);
        Assert.Equal("weapons:laser", Item(blueprint, "Product"));
        Assert.Equal("iron_ingot", (string)((JsonObject)((JsonArray)blueprint.Data["Ingredients"]!)[0]!)["Item"]!);
    }

    [Fact]
    public void ChildPacks_UseTheirParentItems()
    {
        WriteBase();
        _sets.Config("Weapons", "weapons", "*");
        _sets.Write("Weapons/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Laser\" }");
        _sets.Config("Lasers", "lasers", "weapons");
        _sets.Write("Lasers/Items/lens/lens.json", "{ \"ID\": \"lens\", \"Name\": \"Lens\" }");
        _sets.Write("Lasers/Items/lens/blueprint.json", "{ \"Product\": { \"Item\": \"lens\" }, \"Ingredients\": [ { \"Item\": \"laser\" } ] }");
        _sets.Write("Lasers/Items/laser/laser.json", "{ \"ID\": \"laser\", \"Name\": \"Better Laser\" }");

        var content = Read("lasers");

        Assert.Equal("Better Laser", (string)content.Items.Single(i => i.Key == "weapons:laser").Data["Name"]!);
        Assert.DoesNotContain(content.Items, i => i.Key == "lasers:laser");
        var blueprint = Assert.Single(content.Blueprints, b => b.Key == "lasers:lens");
        Assert.Equal("weapons:laser", (string)((JsonObject)((JsonArray)blueprint.Data["Ingredients"]!)[0]!)["Item"]!);
    }
}
