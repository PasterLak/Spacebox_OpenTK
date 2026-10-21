using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipKeyPresetsTests
{
    [Fact]
    public void WithKey_CopiesBindingsAndReplacesOneGroup()
    {
        var bindings = new ConsoleBindings(3, false);

        var keys = ShipKeyPresets.WithKey(bindings, 1, 777);

        Assert.Equal(new[] { bindings.GetKey(0), 777, bindings.GetKey(2) }, keys);
    }

    [Fact]
    public void Presets_RoundTripThroughJson()
    {
        var presets = new ShipKeyPresets();
        presets.Remember("default:ship_console", new[] { 5, 6, 7 });

        var loaded = ShipKeyPresets.Deserialize(presets.Serialize());

        Assert.True(loaded.TryGet("DEFAULT:ship_console", out var keys));
        Assert.Equal(new[] { 5, 6, 7 }, keys);
    }

    [Fact]
    public void Remember_StoresACopy()
    {
        var presets = new ShipKeyPresets();
        var source = new[] { 1, 2 };
        presets.Remember("console", source);
        source[0] = 99;

        Assert.True(presets.TryGet("console", out var keys));
        Assert.Equal(1, keys[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public void Deserialize_BadDataGivesEmptyPresets(string json)
    {
        Assert.False(ShipKeyPresets.Deserialize(json).TryGet("console", out _));
    }
}
