using System.Text.Json;
using System.Text.Json.Nodes;
using SharpNBT;
using Spacebox.Game.Generation.Tools.SaveData;

namespace Spacebox.Tests.Game;

public sealed class JsonNbtConverterTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox_savedata_" + Guid.NewGuid().ToString("N"));

    public JsonNbtConverterTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private static JsonNode RoundTrip(string json) => JsonNbtConverter.ToJson(JsonNbtConverter.ToNbt(JsonNode.Parse(json)));

    [Fact]
    public void Objects_KeepEveryValueType()
    {
        const string json = """
            {"Name":"Anna","Health":87,"Speed":3.25,"Alive":true,"Frozen":false,"Id":-4074653125396582032,
             "Position":{"X":5949.5,"Y":2849,"Z":-5603.25},"Empty":{},"Text":"a \"quoted\" ünïcode"}
            """;
        var result = RoundTrip(json)!.AsObject();

        Assert.Equal("Anna", result["Name"]!.GetValue<string>());
        Assert.Equal(87, result["Health"]!.GetValue<long>());
        Assert.Equal(3.25, result["Speed"]!.GetValue<double>());
        Assert.True(result["Alive"]!.GetValue<bool>());
        Assert.False(result["Frozen"]!.GetValue<bool>());
        Assert.Equal(-4074653125396582032, result["Id"]!.GetValue<long>());
        Assert.Equal(-5603.25, result["Position"]!["Z"]!.GetValue<double>());
        Assert.Empty(result["Empty"]!.AsObject());
        Assert.Equal("a \"quoted\" ünïcode", result["Text"]!.GetValue<string>());
    }

    [Fact]
    public void Arrays_OfEveryShapeSurvive()
    {
        var result = RoundTrip("""{"Ints":[1,2,3],"Mixed":[1,2.5,3],"Objects":[{"A":1},{"A":2}],"None":[],"Strings":["x","y"],"Odd":[1,"two",null,{"B":true}],"Nested":[[1,2],[3]]}""")!.AsObject();

        Assert.Equal(new long[] { 1, 2, 3 }, result["Ints"]!.AsArray().Select(n => n!.GetValue<long>()));
        Assert.Equal(new[] { 1.0, 2.5, 3.0 }, result["Mixed"]!.AsArray().Select(n => n!.GetValue<double>()));
        Assert.Equal(2, result["Objects"]![1]!["A"]!.GetValue<long>());
        Assert.Empty(result["None"]!.AsArray());
        Assert.Equal("y", result["Strings"]![1]!.GetValue<string>());
        var odd = result["Odd"]!.AsArray();
        Assert.Equal(1, odd[0]!.GetValue<long>());
        Assert.Equal("two", odd[1]!.GetValue<string>());
        Assert.Null(odd[2]);
        Assert.True(odd[3]!["B"]!.GetValue<bool>());
        Assert.Equal(3, result["Nested"]![1]![0]!.GetValue<long>());
    }

    [Fact]
    public void RootArray_IsWrappedAndRestored()
    {
        var result = RoundTrip("""[{"Name":"Base","X":1.5}]""")!.AsArray();
        Assert.Single(result);
        Assert.Equal("Base", result[0]!["Name"]!.GetValue<string>());
    }

    [Fact]
    public void NbtFile_HoldsRealTags()
    {
        string path = Path.Combine(_folder, "player.player");
        SaveDataFile.WriteJson(path, """{"Health":50,"Name":"Anna","Slots":[{"Id":3,"Count":10}]}""");

        var tag = NbtFile.Read(path, FormatOptions.Java, CompressionType.AutoDetect);
        Assert.IsType<LongTag>(tag["Health"]);
        Assert.IsType<StringTag>(tag["Name"]);
        Assert.IsType<ListTag>(tag["Slots"]);
    }

    [Fact]
    public void Dto_RoundTripsThroughAFile()
    {
        string path = Path.Combine(_folder, "rules.nbt");
        var original = new SampleDto { Name = "World", Seed = 123456789012L, Scale = 0.1f, Enabled = true, Ids = new List<long> { 5, -7 }, Tags = new Dictionary<string, int> { ["a b"] = 2 } };
        SaveDataFile.Write(path, original);

        var loaded = SaveDataFile.Read<SampleDto>(path);
        Assert.Equal("World", loaded.Name);
        Assert.Equal(123456789012L, loaded.Seed);
        Assert.Equal(0.1f, loaded.Scale);
        Assert.True(loaded.Enabled);
        Assert.Equal(new List<long> { 5, -7 }, loaded.Ids);
        Assert.Equal(2, loaded.Tags["a b"]);
        Assert.Null(loaded.Missing);
    }

    [Fact]
    public void LegacyJson_IsReadUntilTheFirstSaveRemovesIt()
    {
        string path = Path.Combine(_folder, "tags.nbt");
        string legacy = Path.Combine(_folder, "tags.json");
        File.WriteAllText(legacy, """[{"Name":"Home"}]""");

        Assert.Contains("Home", SaveDataFile.ReadJson(path, legacy));

        SaveDataFile.WriteJson(path, SaveDataFile.ReadJson(path, legacy), legacy);
        Assert.False(File.Exists(legacy));
        Assert.True(File.Exists(path));
        Assert.Contains("Home", SaveDataFile.ReadJson(path, legacy));
    }

    private sealed class SampleDto
    {
        public string Name { get; set; }
        public long Seed { get; set; }
        public float Scale { get; set; }
        public bool Enabled { get; set; }
        public List<long> Ids { get; set; }
        public Dictionary<string, int> Tags { get; set; }
        public string Missing { get; set; }
    }
}
