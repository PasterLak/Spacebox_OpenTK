using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public class ContainerAccessRulesTests
{
    private static List<ItemAmount> Deltas(params int[] amounts)
    {
        var list = new List<ItemAmount>();
        foreach (var amount in amounts) list.Add(new ItemAmount { ItemId = 1, Amount = amount });
        return list;
    }

    [Fact]
    public void CreativeInventory_MayDeposit()
    {
        Assert.True(ContainerAccessRules.Allows(true, true, Deltas(5, -2)));
    }

    [Fact]
    public void UntrackedWithoutCreativeInventory_MayOnlyWithdraw()
    {
        Assert.True(ContainerAccessRules.Allows(true, false, Deltas(-3)));
        Assert.False(ContainerAccessRules.Allows(true, false, Deltas(-3, 1)));
    }

    [Fact]
    public void ModesWithoutItems_MayNotChangeContainers()
    {
        Assert.False(ContainerAccessRules.Allows(false, false, Deltas(-1)));
        Assert.True(ContainerAccessRules.Allows(false, false, Deltas()));
    }
}
