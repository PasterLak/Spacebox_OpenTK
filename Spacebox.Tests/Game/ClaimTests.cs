using System.Text.Json;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Protection;
using Spacebox.Game.Resource;
using Spacebox.Network;
using Spacebox.Network.Dedicated;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Game;

public class ClaimTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "spacebox-claims-" + Guid.NewGuid().ToString("N"));

    public ClaimTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private static ClaimBeaconBlock NewBeacon() => new ClaimBeaconBlock(new ClaimBeaconBlockData(new BlockData("claim_beacon", "claim")));

    [Fact]
    public void Zone_ContainsCellsWithinTheRadius()
    {
        var center = new Vector3i(10, 10, 10);

        Assert.True(ClaimZone.Contains(center, 12, new Vector3i(22, -2, 10)));
        Assert.False(ClaimZone.Contains(center, 12, new Vector3i(23, 10, 10)));
        Assert.True(ClaimZone.Overlap(Vector3i.Zero, 12, new Vector3i(24, 0, 0), 12));
        Assert.False(ClaimZone.Overlap(Vector3i.Zero, 12, new Vector3i(25, 0, 0), 12));
        Assert.True(ClaimZone.BoxIntersects(Vector3i.Zero, 2, new Vector3i(2, 2, 2), new Vector3i(5, 5, 5)));
        Assert.False(ClaimZone.BoxIntersects(Vector3i.Zero, 2, new Vector3i(3, 0, 0), new Vector3i(5, 1, 1)));
    }

    [Fact]
    public void Membership_IgnoresCaseAndKeepsTheOwnerOut()
    {
        var members = new List<string>();

        Assert.True(ClaimMembership.TryAdd(members, "Owner", " Anna ", 2));
        Assert.False(ClaimMembership.TryAdd(members, "Owner", "ANNA", 2));
        Assert.False(ClaimMembership.TryAdd(members, "Owner", "owner", 2));
        Assert.True(ClaimMembership.TryAdd(members, "Owner", "Bob", 2));
        Assert.False(ClaimMembership.TryAdd(members, "Owner", "Carl", 2));

        Assert.True(ClaimMembership.IsAllowed("Owner", members, "owner"));
        Assert.True(ClaimMembership.IsAllowed("Owner", members, "bob"));
        Assert.False(ClaimMembership.IsAllowed("Owner", members, "Carl"));
        Assert.False(ClaimMembership.IsAllowed("Owner", members, ""));

        Assert.True(ClaimMembership.Remove(members, "anna"));
        Assert.Equal(new[] { "Bob" }, members);
        Assert.Equal(ClaimSettings.MaxNameLength, ClaimMembership.Sanitize(new string('x', 50)).Length);
    }

    [Theory]
    [InlineData(1, 50, 1f)]
    [InlineData(2, 50, 1.5f)]
    [InlineData(3, 50, 2f)]
    [InlineData(0, 50, 1f)]
    [InlineData(4, 0, 1f)]
    public void BurnRate_GrowsWithTheNumberOfClaims(int claims, int percent, float expected)
    {
        Assert.Equal(expected, ClaimCost.BurnRate(claims, percent), 3);
    }

    [Fact]
    public void Payments_UseShortOrFullIdsAndRespectTheCap()
    {
        var settings = new ClaimSettings();

        Assert.Equal(3600, ClaimCost.SecondsFor(settings, "default:cobalt_ingot"));
        Assert.Equal(7 * 24 * 3600, ClaimCost.SecondsFor(settings, "obscurium_ingot"));
        Assert.Equal(0, ClaimCost.SecondsFor(settings, "default:glass"));
        Assert.True(ClaimCost.CanAdd(0f, 3600, 7200));
        Assert.False(ClaimCost.CanAdd(5000f, 3600, 7200));
        Assert.False(ClaimCost.CanAdd(0f, 0, 7200));
        Assert.Equal("2d 3h", ClaimCost.FormatDuration(2 * 86400 + 3 * 3600));
        Assert.Equal("1h 30m", ClaimCost.FormatDuration(5400));
        Assert.Equal("0m", ClaimCost.FormatDuration(0));
        Assert.Equal("1h", ClaimCost.FormatDuration(3600));
        Assert.Equal("14d", ClaimCost.FormatDuration(14 * 86400));
        Assert.Equal("1d 2h", ClaimCost.FormatDuration(26 * 3600));
    }

    [Fact]
    public void Settings_DefaultsAndValidation()
    {
        var settings = JsonSerializer.Deserialize<ClaimSettings>("{ \"Radius\": 500, \"MaxStoredMinutes\": 10, \"Payments\": { \"cobalt_ingot\": 30, \"bad\": -5 } }")!;

        Assert.True(settings.Validate());
        Assert.Equal(ClaimSettings.Defaults.Radius, settings.Radius);
        Assert.Equal(ClaimSettings.Defaults.MaxStoredMinutes, settings.MaxStoredMinutes);
        Assert.Single(settings.Payments);
        Assert.Equal(30, ClaimCost.MinutesFor(settings, "COBALT_INGOT"));
        Assert.False(new ClaimSettings().Validate());
    }

    [Fact]
    public void ServerConfig_WithoutClaims_UsesDefaults()
    {
        var config = JsonSerializer.Deserialize<ServerConfig>("{ \"MaxPlayers\": 4 }")!;

        Assert.NotNull(config.Claims);
        Assert.True(config.Claims.Enabled);
        Assert.Equal(7, config.Claims.Payments.Count);
        Assert.Equal(30, ClaimCost.MinutesFor(config.Claims, "default:iron_ingot"));
        Assert.Equal(600, ClaimCost.MinutesFor(config.Claims, "titanium_ingot"));
    }

    [Fact]
    public void Settings_TravelAsJson()
    {
        var settings = new ClaimSettings { Radius = 20, ExtraClaimCostPercent = 75 };

        var copy = ClaimSettings.FromJson(settings.ToJson());

        Assert.Equal(20, copy.Radius);
        Assert.Equal(75, copy.ExtraClaimCostPercent);
        Assert.Equal(settings.Payments.Count, copy.Payments.Count);
        Assert.Equal(ClaimSettings.Defaults.Radius, ClaimSettings.FromJson("not json").Radius);
    }

    [Fact]
    public void Index_CountsClaimsPerOwnerAndPersists()
    {
        var index = new ClaimIndex();
        index.Load(_folder);
        index.Upsert(ClaimIndex.KeyFor(1, new Vector3i(1, 2, 3)), "Anna", true);
        index.Upsert(ClaimIndex.KeyFor(2, Vector3i.Zero), "anna", false);
        index.Upsert(ClaimIndex.KeyFor(3, Vector3i.Zero), "Bob", true);

        var reloaded = new ClaimIndex();
        reloaded.Load(_folder);

        Assert.Equal(1, reloaded.CountActive("ANNA"));
        Assert.Equal(2, reloaded.CountAll("Anna"));
        Assert.True(reloaded.Remove(ClaimIndex.KeyFor(1, new Vector3i(1, 2, 3))));
        Assert.Equal(0, reloaded.CountActive("Anna"));
    }

    [Fact]
    public void Beacon_ActiveOnlyWithOwnerAndTime()
    {
        var beacon = NewBeacon();
        Assert.False(beacon.IsClaimActive);

        beacon.Restore("Anna", new[] { "Bob" }, 100f);
        Assert.True(beacon.IsClaimActive);
        Assert.True(beacon.IsAllowed("bob"));
        Assert.False(beacon.IsAllowed("Carl"));

        beacon.Restore("", null, 100f);
        Assert.False(beacon.IsClaimActive);
    }

    [Fact]
    public void Beacon_PaymentAddsTimeAndTimeRunsOut()
    {
        var beacon = NewBeacon();
        beacon.Restore("Anna", null, 0f);
        var cobalt = new Item(1, 64, "Cobalt Ingot") { Id_string = "default:cobalt_ingot" };
        beacon.PaymentStorage.SetSlot(0, 0, cobalt, 2);

        beacon.Update(1f, 1f);
        Assert.True(beacon.IsClaimActive);
        Assert.InRange(beacon.RemainingSeconds, 3598f, 3600f);
        Assert.Equal(1, beacon.PaymentStorage.GetSlot(0, 0).Count);

        beacon.Restore("Anna", null, 10f);
        beacon.PaymentStorage.GetSlot(0, 0).Clear();
        beacon.Update(6f, 2f);
        Assert.False(beacon.IsClaimActive);
    }

    [Fact]
    public void Beacon_PaymentSlot_AcceptsOnlyPaymentIngots()
    {
        var beacon = NewBeacon();
        var slot = beacon.PaymentStorage.GetSlot(0, 0);

        Assert.True(slot.Filter(new Item(1, 64, "Uranium") { Id_string = "default:uranium_ingot" }));
        Assert.False(slot.Filter(new Item(2, 64, "Glass") { Id_string = "default:glass" }));
    }

    [Fact]
    public void Serializer_RoundTripsOwnerMembersAndTime()
    {
        var beacon = NewBeacon();
        beacon.Restore("Anna", new[] { "Bob", "Carl" }, 1234f);
        var serializer = new ClaimBeaconSerializer();
        short index = 0;

        var tag = serializer.Serialize(beacon, 5, new Dictionary<string, short>(), ref index);
        var copy = NewBeacon();
        serializer.Deserialize(copy, tag, Array.Empty<string>());

        Assert.Equal("Anna", copy.Owner);
        Assert.Equal(new[] { "Bob", "Carl" }, copy.Members);
        Assert.Equal(1234f, copy.RemainingSeconds, 3);
        Assert.Null(serializer.Serialize(NewBeacon(), 1, new Dictionary<string, short>(), ref index));
    }

    [Fact]
    public void NetworkState_RoundTrips()
    {
        var beacon = NewBeacon();
        beacon.Restore("Anna", new[] { "Bob" }, 50f);
        var writer = new NetWriter();
        beacon.WriteNetworkState(writer);

        var copy = NewBeacon();
        copy.ReadNetworkState(new NetReader(writer.ToArray()));

        Assert.Equal("Anna", copy.Owner);
        Assert.Equal(new[] { "Bob" }, copy.Members);
        Assert.Equal(50f, copy.RemainingSeconds, 3);
    }

    [Fact]
    public void Messages_RoundTrip_AndOnlyOwnerOrAdminManages()
    {
        var writer = new NetWriter();
        new ClaimMembersMessage { Key = new ContainerKey(7, new Vector3i(1, 2, 3)), Name = "Bob", Add = true }.Serialize(writer);
        var copy = new ClaimMembersMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));
        Assert.Equal("Bob", copy.Name);
        Assert.True(copy.Add);
        Assert.Equal(new Vector3i(1, 2, 3), copy.Key.Cell);

        var beacon = NewBeacon();
        beacon.Restore("Anna", new[] { "Bob" }, 10f);
        Assert.True(ClaimMembersNetwork.CanManage(beacon, "anna", false));
        Assert.False(ClaimMembersNetwork.CanManage(beacon, "Bob", false));
        Assert.True(ClaimMembersNetwork.CanManage(beacon, "Bob", true));
    }
}
