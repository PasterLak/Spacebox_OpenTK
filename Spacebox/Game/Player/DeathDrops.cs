using Spacebox.Game.Generation;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Rules;

namespace Spacebox.Game.Player;

public static class DeathDrops
{
    public static bool ShouldDrop(ServerRules rules, GameMode mode) => !rules.KeepInventory && mode == GameMode.Survival;

    public static List<ItemStack> TakeAll(params Storage[] storages)
    {
        var stacks = new List<ItemStack>();
        foreach (var storage in storages)
        {
            if (storage == null) continue;

            bool changed = false;
            foreach (var slot in storage.GetAllSlots())
            {
                if (!slot.HasItem || slot.Item == null || slot.Count <= 0) continue;

                Add(stacks, slot.Item, slot.Count);
                changed = true;
            }
            if (changed) storage.Clear();
        }
        return stacks;
    }

    public static void Drop(LocalAstronaut player)
    {
        if (player == null || !ShouldDrop(ServerRules.Current, player.GameMode)) return;

        var drops = World.DropEffectManager;
        if (drops == null) return;

        var stacks = TakeAll(player.Panel, player.Inventory, player.Equipment?.Storage);
        if (stacks.Count == 0) return;

        drops.DropDeathItems(player.Position, stacks);
    }

    private static void Add(List<ItemStack> stacks, Item item, int count)
    {
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].Item.Id != item.Id) continue;
            stacks[i] = stacks[i] with { Count = stacks[i].Count + count };
            return;
        }
        stacks.Add(new ItemStack(item, count));
    }
}
