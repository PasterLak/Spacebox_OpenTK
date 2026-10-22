using Spacebox.Network.Items;

namespace Spacebox.Tests.Network;

public class PlayerInventoryJsonTests
{
    private const string Json = """
    {
      "PositionX": 1,
      "InventorySlots": [ { "ItemID": "default:iron", "Count": 10, "SlotX": 0, "SlotY": 0 }, { "ItemID": "default:unknown", "Count": 3, "SlotX": 1, "SlotY": 0 } ],
      "PanelSlots": [ { "ItemID": "default:iron", "Count": 5, "SlotX": 0, "SlotY": 0 }, { "ItemID": "default:stone", "Count": 2, "SlotX": 1, "SlotY": 0 } ],
      "EquipmentSlots": []
    }
    """;

    private static short? Resolve(string id) => id switch
    {
        "default:iron" => 1,
        "default:stone" => 2,
        _ => null
    };

    [Fact]
    public void Totals_SumAllStoragesAndSkipUnknownItems()
    {
        Assert.True(PlayerInventoryJson.TryReadTotals(Json, Resolve, out var totals, out bool hasEquipment));

        Assert.Equal(15, totals[1]);
        Assert.Equal(2, totals[2]);
        Assert.Equal(2, totals.Count);
        Assert.True(hasEquipment);
    }

    [Fact]
    public void RemoveItems_TakesFromTheSlotsAndKeepsTheRest()
    {
        string corrected = PlayerInventoryJson.RemoveItems(Json, Resolve, new Dictionary<short, int> { [1] = 12, [2] = 2 });

        Assert.True(PlayerInventoryJson.TryReadTotals(corrected, Resolve, out var totals, out _));
        Assert.Equal(3, totals[1]);
        Assert.False(totals.ContainsKey(2));
        Assert.Contains("\"PositionX\": 1", corrected);
    }

    [Fact]
    public void StringFields_RoundTrip()
    {
        string json = PlayerInventoryJson.WriteStrings(Json, PlayerInventoryJson.ClaimedRewardsField, new[] { "basics/step1", "basics/chapter:a" });

        Assert.Equal(new[] { "basics/step1", "basics/chapter:a" }, PlayerInventoryJson.ReadStrings(json, PlayerInventoryJson.ClaimedRewardsField));
        Assert.Empty(PlayerInventoryJson.ReadStrings(Json, PlayerInventoryJson.ClaimedRewardsField));
    }

    [Fact]
    public void BrokenJson_IsNotRead()
    {
        Assert.False(PlayerInventoryJson.TryReadTotals("{ broken", Resolve, out _, out _));
        Assert.False(PlayerInventoryJson.TryReadTotals(null, Resolve, out _, out _));
    }
}
