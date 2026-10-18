using System.Text.Json;
using Engine.Utils;

namespace Engine.Tests.Utils;

public class JsonFixerTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = "";
        public int[] Values { get; set; } = [];
    }

    [Fact]
    public void FixJson_RemovesTrailingCommas()
    {
        string fixedJson = JsonFixer.FixJson("{ \"Name\": \"a\", \"Values\": [1, 2,], }");

        var sample = JsonSerializer.Deserialize<Sample>(fixedJson)!;

        Assert.Equal("a", sample.Name);
        Assert.Equal(new[] { 1, 2 }, sample.Values);
    }

    [Fact]
    public void FixJson_QuotesUnquotedKeys()
    {
        string fixedJson = JsonFixer.FixJson("{ Name: \"b\", Values: [3] }");

        var sample = JsonSerializer.Deserialize<Sample>(fixedJson)!;

        Assert.Equal("b", sample.Name);
        Assert.Equal(new[] { 3 }, sample.Values);
    }

    [Fact]
    public void LoadJsonSafe_ReadsValidAndFixableFiles()
    {
        string valid = Path.GetTempFileName();
        string fixable = Path.GetTempFileName();
        try
        {
            File.WriteAllText(valid, "{ \"name\": \"ok\", // comment\n \"values\": [1] }");
            File.WriteAllText(fixable, "{ \"Name\": \"fixed\", \"Values\": [1,], }");

            Assert.Equal("ok", JsonFixer.LoadJsonSafe<Sample>(valid)!.Name);
            Assert.Equal("fixed", JsonFixer.LoadJsonSafe<Sample>(fixable)!.Name);
        }
        finally
        {
            File.Delete(valid);
            File.Delete(fixable);
        }
    }

    [Fact]
    public void LoadJsonSafe_ReturnsNullForBrokenOrMissingFile()
    {
        string broken = Path.GetTempFileName();
        try
        {
            File.WriteAllText(broken, "{ \"Name\": \"x\" }after ]");

            Assert.Null(JsonFixer.LoadJsonSafe<Sample>(broken));
            Assert.Null(JsonFixer.LoadJsonSafe<Sample>(broken + ".missing"));
        }
        finally
        {
            File.Delete(broken);
        }
    }
}
