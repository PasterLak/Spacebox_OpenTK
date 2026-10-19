using Spacebox.Game;
using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game;

public class ArmorProtectionTests
{
    private static readonly (EquipmentSlotType Slot, int Weight)[] ArmorSlots =
    {
        (EquipmentSlotType.Helmet, 1), (EquipmentSlotType.Chest, 3), (EquipmentSlotType.Shoulders, 2),
        (EquipmentSlotType.Arms, 1), (EquipmentSlotType.Legs, 2), (EquipmentSlotType.Boots, 1)
    };

    private static PlayerEquipment Wear(int multiplier)
    {
        var equipment = new PlayerEquipment();
        foreach (var (slot, weight) in ArmorSlots)
        {
            var item = new EquipmentItem(slot.ToString(), 1f) { Slot = slot, ExplosionProtection = ArmorProtection.FromPercent(weight * multiplier) };
            Assert.True(equipment.TryEquipIfEmpty(item));
        }
        return equipment;
    }

    [Theory]
    [InlineData(1, 0.1f)]
    [InlineData(3, 0.3f)]
    [InlineData(5, 0.5f)]
    public void FullSet_SumsPieces(int multiplier, float expected)
    {
        Assert.Equal(expected, Wear(multiplier).ExplosionProtection, 4);
    }

    [Fact]
    public void Protection_IsCapped()
    {
        Assert.Equal(ArmorProtection.MaxExplosionProtection, Wear(10).ExplosionProtection, 4);
    }

    [Fact]
    public void Apply_ReducesDamage()
    {
        Assert.Equal(100, ArmorProtection.Apply(200, 0.5f));
        Assert.Equal(27, ArmorProtection.Apply(30, 0.1f));
        Assert.Equal(1, ArmorProtection.Apply(1, 0.5f));
        Assert.Equal(0, ArmorProtection.Apply(0, 0.5f));
        Assert.Equal(200, ArmorProtection.Apply(200, 0f));
    }

    [Fact]
    public void FromPercent_Clamps()
    {
        Assert.Equal(0f, ArmorProtection.FromPercent(-5f));
        Assert.Equal(1f, ArmorProtection.FromPercent(250f));
        Assert.Equal(0.15f, ArmorProtection.FromPercent(15f), 4);
    }
}
