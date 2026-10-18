using Spacebox.Game;
using Spacebox.Game.GUI;

namespace Spacebox.Tests;

public class VersionUpgradeTests
{
    private static WorldInfo World(string version)
    {
        return new WorldInfo { Name = "W", GameVersion = version };
    }

    [Theory]
    [InlineData("0.0.8")]
    [InlineData("0.0.9")]
    [InlineData("0.1.0")]
    [InlineData("0.1.1")]
    public void Convert_UpgradesEveryKnownVersionToCurrent(string version)
    {
        var world = World(version);

        Assert.True(VersionConverter.Convert(world, Engine.Application.Version));
        Assert.Equal(Engine.Application.Version, world.GameVersion);
    }

    [Fact]
    public void Convert_StopsAtTargetVersion()
    {
        var world = World("0.0.8");

        Assert.True(VersionConverter.Convert(world, "0.1.0"));
        Assert.Equal("0.1.0", world.GameVersion);
    }

    [Fact]
    public void Convert_NewerWorld_FailsWithoutChange()
    {
        var world = World("9.0.0");

        Assert.False(VersionConverter.Convert(world, "0.1.2"));
        Assert.Equal("9.0.0", world.GameVersion);
    }

    [Fact]
    public void Convert_UnknownOlderVersion_FailsWithoutChange()
    {
        var world = World("0.0.5");

        Assert.False(VersionConverter.Convert(world, "0.1.2"));
        Assert.Equal("0.0.5", world.GameVersion);
    }
}
