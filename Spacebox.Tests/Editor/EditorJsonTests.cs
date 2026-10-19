using System.Text.Json.Nodes;
using Spacebox.Editor.Data;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Editor;

public class EditorJsonTests
{
    [Fact]
    public void Parse_AcceptsCommentsAndTrailingCommas()
    {
        var node = EditorJsonFile.Parse("{ // comment\n \"a\": 1, }");

        Assert.Equal(1, node["a"]!.GetValue<int>());
    }

    [Fact]
    public void FindKey_IsCaseInsensitive()
    {
        var obj = new JsonObject { ["ID"] = "x" };

        Assert.Equal("ID", EditorJson.FindKey(obj, "id"));
        Assert.Equal("x", EditorJson.GetString(obj, "Id"));
    }

    [Fact]
    public void Set_KeepsExistingKeyCasing()
    {
        var obj = new JsonObject { ["ID"] = "x" };

        EditorJson.Set(obj, "id", JsonValue.Create("y"));

        Assert.Single(obj);
        Assert.Equal("y", obj["ID"]!.GetValue<string>());
    }

    [Fact]
    public void MakeUniqueId_AddsCounter()
    {
        var used = new HashSet<string> { "block", "block_2" };

        Assert.Equal("block_3", EditorJson.MakeUniqueId("block", used.Contains));
        Assert.Equal("other", EditorJson.MakeUniqueId("other", used.Contains));
    }

    [Fact]
    public void HasKeys_DetectsColorObjects()
    {
        Assert.True(EditorJson.HasKeys(new JsonObject { ["r"] = 1, ["g"] = 2, ["b"] = 3 }, "r", "g", "b"));
        Assert.False(EditorJson.HasKeys(new JsonObject { ["r"] = 1, ["g"] = 2 }, "r", "g", "b"));
    }

    [Fact]
    public void Schema_ReadsDefaultsAndNames()
    {
        var speed = JsonSchema.Find(typeof(ProjectileJSON), "speed");

        Assert.NotNull(speed);
        Assert.Equal(5, speed.Default!.GetValue<float>());
        Assert.NotNull(JsonSchema.Find(typeof(LootEntry), "ID"));
        Assert.Null(JsonSchema.Find(typeof(LootEntry), "ItemIdStr"));
    }

    [Fact]
    public void Schema_ResolvesCollections()
    {
        Assert.Equal(typeof(LootEntry), JsonSchema.ElementType(typeof(List<LootEntry>)));
        Assert.Equal(typeof(string), JsonSchema.ElementType(typeof(string[])));
        Assert.Null(JsonSchema.ElementType(typeof(Dictionary<string, byte>)));
        Assert.Equal(typeof(byte), JsonSchema.DictionaryValueType(typeof(Dictionary<string, byte>)));
    }

    [Fact]
    public void Schema_CreateEmpty_BuildsColorAndCollections()
    {
        var color = JsonSchema.CreateEmpty(typeof(Engine.Color3Byte));

        Assert.True(EditorJson.HasKeys(color, "r", "g", "b"));
        Assert.IsType<JsonArray>(JsonSchema.CreateEmpty(typeof(List<int>)));
        Assert.IsType<JsonObject>(JsonSchema.CreateEmpty(typeof(Dictionary<string, int>)));
    }

    [Fact]
    public void Catalog_PicksItemSchemaByType()
    {
        Assert.Equal(typeof(WeaponItemJSON), GameSetCatalog.ItemSchema(new JsonObject { ["Type"] = "weapon" }));
        Assert.Equal(typeof(ItemJSON), GameSetCatalog.ItemSchema(new JsonObject { ["Type"] = "unknown" }));
        Assert.Equal(typeof(ProjectileJSON), GameSetCatalog.FileSchema("Projectiles/p_ar.json"));
        Assert.Equal(typeof(List<LootEntry>), GameSetCatalog.FileSchema("Generation/Loot/chest.json"));
    }
}
