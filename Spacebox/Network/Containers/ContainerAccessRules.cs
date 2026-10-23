using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public static class ContainerAccessRules
{
    public static bool Allows(bool useItems, bool creativeInventory, List<ItemAmount> deltas)
    {
        if (deltas == null || deltas.Count == 0) return true;
        if (!useItems) return false;
        if (creativeInventory) return true;

        for (int i = 0; i < deltas.Count; i++)
            if (deltas[i].Amount > 0) return false;
        return true;
    }
}
