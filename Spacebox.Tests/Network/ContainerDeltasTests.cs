using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Tests.Network;

public sealed class ContainerDeltasTests
{
    private static ItemAmount A(short id, int amount) => new ItemAmount { ItemId = id, Amount = amount };

    [Fact]
    public void RepeatedItems_AreSummedSoTheyCannotCancelChecks()
    {
        var result = ContainerDeltas.Normalize(new List<ItemAmount> { A(5, -64), A(5, 64), A(7, 3), A(7, 2) });

        Assert.Equal(new[] { A(7, 5) }, result);
    }

    [Fact]
    public void ZeroAndMissingLists_GiveNoChanges()
    {
        Assert.Empty(ContainerDeltas.Normalize(null));
        Assert.Empty(ContainerDeltas.Normalize(new List<ItemAmount> { A(1, 0) }));
    }

    [Fact]
    public void HugeSums_AreClampedInsteadOfOverflowing()
    {
        var result = ContainerDeltas.Normalize(new List<ItemAmount> { A(1, int.MaxValue), A(1, int.MaxValue) });

        Assert.Equal(int.MaxValue, result.Single().Amount);
    }
}
