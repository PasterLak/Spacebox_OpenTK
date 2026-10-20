using Spacebox.Game.GUI;
using Spacebox.Game;

namespace Spacebox.Tests;

public class EquipmentLayoutTests
{
    [Fact]
    public void Layout_ShowsEverySlotOnce()
    {
        var shown = EquipmentUI.ArmorLayout.Select(p => p.Type).Concat(EquipmentUI.DeviceSlots).ToList();

        Assert.Equal(shown.Count, shown.Distinct().Count());
        Assert.Equal(Enum.GetValues<EquipmentSlotType>().OrderBy(t => t), shown.OrderBy(t => t));
    }

    [Fact]
    public void Layout_PlacesHelmetAboveChestAboveLegsAboveBoots()
    {
        int Row(EquipmentSlotType type) => EquipmentUI.ArmorLayout.Single(p => p.Type == type).Row;

        Assert.True(Row(EquipmentSlotType.Helmet) < Row(EquipmentSlotType.Chest));
        Assert.True(Row(EquipmentSlotType.Chest) < Row(EquipmentSlotType.Legs));
        Assert.True(Row(EquipmentSlotType.Legs) < Row(EquipmentSlotType.Boots));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(370f, 10f)]
    [InlineData(-30f, 330f)]
    [InlineData(-720f, 0f)]
    public void Yaw_IsNormalized(float yaw, float expected)
    {
        Assert.Equal(expected, PlayerPreview.NormalizeYaw(yaw), 3);
    }
}
