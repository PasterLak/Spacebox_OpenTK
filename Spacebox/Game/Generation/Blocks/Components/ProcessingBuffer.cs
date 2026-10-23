namespace Spacebox.Game.Generation.Blocks.Components;

public static class ProcessingBuffer
{
    public static void PushOutput(ItemSlot output, Storage buffer)
    {
        if (!output.HasItem) return;

        byte count = output.Count;
        buffer.TryAddItem(output.Item, count, out byte rest);
        if (rest != count) output.Count = rest;
    }

    public static void PullInput(Storage input, Storage buffer, string recipeType)
    {
        var inSlot = input.GetSlot(0, 0);

        for (int x = 0; x < buffer.SizeX; x++)
        {
            for (int y = 0; y < buffer.SizeY; y++)
            {
                var slot = buffer.GetSlot(x, y);
                if (!slot.HasItem || !Accepts(inSlot, slot.Item, recipeType)) continue;

                var item = slot.Item;
                int space = item.StackSize - (inSlot.HasItem ? inSlot.Count : 0);
                if (space <= 0) return;

                byte moved = (byte)Math.Min(space, slot.Count);
                slot.Count -= moved;

                if (inSlot.HasItem) inSlot.Count += moved;
                else input.SetSlot(0, 0, item, moved);

                if (inSlot.Count >= item.StackSize) return;
            }
        }
    }

    private static bool Accepts(ItemSlot inSlot, Item item, string recipeType)
    {
        if (inSlot.HasItem) return inSlot.Item.Id == item.Id;
        return GameAssets.TryGetRecipe(recipeType, item.Id, out _);
    }
}
