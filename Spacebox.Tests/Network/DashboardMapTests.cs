using OpenTK.Mathematics;
using Spacebox.Network.Dashboard.Map;

namespace Spacebox.Tests.Network;

public class DashboardMapTests
{
    private static readonly Dictionary<long, MapClaimRecord> NoClaims = new Dictionary<long, MapClaimRecord>();

    [Fact]
    public void Build_KeepsOnlyEntitiesNearPlayers()
    {
        var players = new[] { new MapPlayerRecord("a", new Vector3(0, 0, 0), true), new MapPlayerRecord("b", new Vector3(10000, 0, 0), true) };
        var entities = new[]
        {
            new MapEntityRecord(1, new Vector3(100, 0, 0), 20, false, false, MapPersistence.Regenerated),
            new MapEntityRecord(2, new Vector3(5000, 0, 0), 20, false, false, MapPersistence.Regenerated),
            new MapEntityRecord(3, new Vector3(10200, 0, 0), 20, false, true, MapPersistence.Saved)
        };

        var snapshot = new MapSnapshotBuilder().Build(players, entities, NoClaims, 1000f);

        Assert.Equal(new[] { "1", "3" }, snapshot.Ids);
        Assert.Equal(new double[] { 5000, 0, 0 }, snapshot.Center);
        Assert.Equal(-4900f, snapshot.Entities[0][0]);
        Assert.Equal(2, snapshot.Players.Count);
        Assert.Equal(-5000f, snapshot.Players[0].Position[0]);
    }

    [Fact]
    public void Build_WithFocus_UsesRadiusAroundFocus()
    {
        var players = new[] { new MapPlayerRecord("a", Vector3.Zero, true) };
        var entities = new[]
        {
            new MapEntityRecord(1, new Vector3(100, 0, 0), 20, false, false, MapPersistence.Regenerated),
            new MapEntityRecord(2, new Vector3(5000, 0, 0), 20, false, false, MapPersistence.Regenerated)
        };

        var snapshot = new MapSnapshotBuilder().Build(players, entities, NoClaims, 500f, new Vector3(5100, 0, 0));

        Assert.Equal(new[] { "2" }, snapshot.Ids);
    }

    [Fact]
    public void Build_EncodesFlagsAndClaims()
    {
        var players = new[] { new MapPlayerRecord("a", Vector3.Zero, true) };
        var entities = new[]
        {
            new MapEntityRecord(-7, new Vector3(1, 0, 0), 5, true, true, MapPersistence.Modified),
            new MapEntityRecord(8, new Vector3(2, 0, 0), 30, false, false, MapPersistence.Saved),
            new MapEntityRecord(9, new Vector3(3, 0, 0), 30, false, false, MapPersistence.Regenerated)
        };
        var claims = new Dictionary<long, MapClaimRecord>
        {
            { -7, new MapClaimRecord("Bob", true) },
            { 8, new MapClaimRecord("bob", false) }
        };

        var snapshot = new MapSnapshotBuilder().Build(players, entities, claims, 1000f);

        int ship = (int)snapshot.Entities[0][4];
        Assert.True((ship & MapSnapshot.FlagShip) != 0);
        Assert.True((ship & MapSnapshot.FlagLoaded) != 0);
        Assert.True((ship & MapSnapshot.FlagClaimActive) != 0);
        Assert.Equal((int)MapPersistence.Modified, ship >> MapSnapshot.PersistenceShift);

        int saved = (int)snapshot.Entities[1][4];
        Assert.True((saved & MapSnapshot.FlagClaimed) != 0);
        Assert.False((saved & MapSnapshot.FlagClaimActive) != 0);
        Assert.Equal((int)MapPersistence.Saved, saved >> MapSnapshot.PersistenceShift);

        Assert.Equal(0, (int)snapshot.Entities[2][4] & (MapSnapshot.FlagClaimed | MapSnapshot.FlagShip));
        Assert.Equal(-1f, snapshot.Entities[2][5]);
        Assert.Single(snapshot.Owners);
        Assert.Equal(0f, snapshot.Entities[1][5]);
    }

    [Fact]
    public void Build_ClampsRadiusAndCapsEntities()
    {
        var players = new[] { new MapPlayerRecord("a", Vector3.Zero, true) };
        var entities = new List<MapEntityRecord>();
        for (int i = 0; i < MapSnapshotBuilder.MaxEntities + 5; i++)
            entities.Add(new MapEntityRecord(i, new Vector3(i % 50, 0, 0), 1, false, false, MapPersistence.Regenerated));

        var snapshot = new MapSnapshotBuilder().Build(players, entities, NoClaims, 1f);

        Assert.Equal(MapSnapshotBuilder.MinRadius, snapshot.Radius);
        Assert.Equal(MapSnapshotBuilder.MaxEntities, snapshot.Entities.Count);
        Assert.Equal(5, snapshot.Truncated);
    }

    [Theory]
    [InlineData("123:1:2:3", 123L)]
    [InlineData("-55:0:0:0", -55L)]
    [InlineData("77", 77L)]
    public void TryParseClaimEntity_ReadsEntityId(string key, long expected)
    {
        Assert.True(MapSnapshotBuilder.TryParseClaimEntity(key, out long id));
        Assert.Equal(expected, id);
    }

    [Fact]
    public void TryParseClaimEntity_RejectsGarbage()
    {
        Assert.False(MapSnapshotBuilder.TryParseClaimEntity("", out _));
        Assert.False(MapSnapshotBuilder.TryParseClaimEntity("abc:1", out _));
    }
}
