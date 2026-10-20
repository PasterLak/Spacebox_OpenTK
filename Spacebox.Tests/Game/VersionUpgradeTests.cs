using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Versioning;

namespace Spacebox.Tests;

public class VersionUpgradeTests
{
    private static WorldInfo World(string version)
    {
        return new WorldInfo { Name = "W", GameVersion = version };
    }

    [Fact]
    public void CurrentVersion_Is015()
    {
        Assert.Equal("0.1.5", Engine.Application.Version);
    }

    [Fact]
    public void Convert_UpgradesTheFirstSupportedVersionToCurrent()
    {
        var world = World(WorldUpgradeRegistry.DefaultMinimumSupportedVersion);

        Assert.True(VersionConverter.Convert(world, Engine.Application.Version));
        Assert.Equal(Engine.Application.Version, world.GameVersion);
    }

    [Theory]
    [InlineData("0.0.8")]
    [InlineData("0.0.9")]
    [InlineData("0.1.0")]
    [InlineData("0.1.1")]
    public void Convert_RefusesVersionsBeforeTheFirstSupported(string version)
    {
        var world = World(version);

        Assert.Equal(WorldUpgradeStatus.TooOld, VersionConverter.GetSupport(world, out var reason));
        Assert.Contains(WorldUpgradeRegistry.DefaultMinimumSupportedVersion, reason);
        Assert.False(VersionConverter.Convert(world, Engine.Application.Version));
        Assert.Equal(version, world.GameVersion);
    }

    [Fact]
    public void GetSupport_ReportsUpToDateAndConvertible()
    {
        Assert.Equal(WorldUpgradeStatus.UpToDate, VersionConverter.GetSupport(World(Engine.Application.Version), out _));
        Assert.Equal(WorldUpgradeStatus.Ok, VersionConverter.GetSupport(World("0.1.2"), out _));
        Assert.Equal(WorldUpgradeStatus.InvalidVersion, VersionConverter.GetSupport(World(""), out _));
    }

    [Fact]
    public void Convert_NewerWorld_FailsWithoutChange()
    {
        var world = World("9.0.0");

        Assert.Equal(WorldUpgradeStatus.Newer, VersionConverter.GetSupport(world, out _));
        Assert.False(VersionConverter.Convert(world, Engine.Application.Version));
        Assert.Equal("9.0.0", world.GameVersion);
    }

    [Fact]
    public void Convert_SkippedVersionsInBetween_HaveNoPath()
    {
        var world = World("0.1.3");

        Assert.Equal(WorldUpgradeStatus.NoPath, VersionConverter.GetSupport(world, out _));
        Assert.Equal("0.1.3", world.GameVersion);
    }
}
