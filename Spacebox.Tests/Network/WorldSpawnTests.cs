using System.Text.Json;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Commands;
using Spacebox.Game.GUI;
using Spacebox.Game.Player.Respawn;
using Spacebox.Network;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class WorldSpawnTests
{
    [Fact]
    public void StoredSpawn_RoundTrips()
    {
        var stored = WorldSpawn.ToStored(new Vector3(1.5f, -2f, 300f));

        Assert.True(WorldSpawn.TryRead(stored, out var position));
        Assert.Equal(new Vector3(1.5f, -2f, 300f), position);
    }

    [Fact]
    public void MissingOrBrokenSpawn_IsNotUsed()
    {
        Assert.False(WorldSpawn.TryRead(null, out _));
        Assert.False(WorldSpawn.TryRead(new[] { 1f, 2f }, out _));
        Assert.False(WorldSpawn.TryRead(new[] { 1f, float.NaN, 3f }, out _));
    }

    [Fact]
    public void OldWorldJson_HasNoSpawnPoint()
    {
        var info = JsonSerializer.Deserialize<WorldInfo>("{ \"Name\": \"Old\", \"Seed\": \"1\" }")!;

        Assert.Null(info.SpawnPoint);
    }

    [Fact]
    public void WorldJson_KeepsTheSpawnPoint()
    {
        var info = new WorldInfo { Name = "W", SpawnPoint = new[] { 10f, 20f, 30f } };

        var copy = JsonSerializer.Deserialize<WorldInfo>(JsonSerializer.Serialize(info))!;

        Assert.Equal(new[] { 10f, 20f, 30f }, copy.SpawnPoint);
    }

    [Theory]
    [InlineData(new[] { "1", "2.5", "-3" }, true)]
    [InlineData(new[] { "1", "2" }, false)]
    [InlineData(new[] { "a", "2", "3" }, false)]
    public void SetSpawn_ParsesCoordinates(string[] args, bool expected)
    {
        Assert.Equal(expected, SetSpawnCommand.TryParsePosition(args, out var position));
        if (expected) Assert.Equal(new Vector3(1f, 2.5f, -3f), position);
    }

    [Fact]
    public void Messages_CarryTheSpawnPoint()
    {
        var spawn = RoundTrip(new WorldSpawnMessage { HasSpawnPoint = true, SpawnPoint = new Vector3(4, 5, 6) });
        Assert.True(spawn.HasSpawnPoint);
        Assert.Equal(new Vector3(4, 5, 6), spawn.SpawnPoint);

        var join = RoundTrip(new JoinAcceptedMessage
        {
            ServerName = "S", WorldName = "W", Seed = "1", ModId = "default", PlayerJson = "", GameSetHash = "", GameSetFolder = "",
            HasSpawnPoint = true, SpawnPoint = new Vector3(7, 8, 9)
        });
        Assert.True(join.HasSpawnPoint);
        Assert.Equal(new Vector3(7, 8, 9), join.SpawnPoint);
        Assert.Equal("W", join.WorldName);
    }

    private static T RoundTrip<T>(T message) where T : struct, INetMessage
    {
        var writer = new NetWriter();
        message.Serialize(writer);
        var copy = new T();
        copy.Deserialize(new NetReader(writer.ToArray()));
        return copy;
    }
}
