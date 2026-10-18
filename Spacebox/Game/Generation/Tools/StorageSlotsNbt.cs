namespace Spacebox.Game.Generation.Tools;

public static class StorageSlotsNbt
{
    public static long[] Write(Storage storage, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var slotsData = new List<long>();
        foreach (var slot in storage.GetAllSlots())
        {
            if (slot == null || !slot.HasItem) continue;

            if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;
            slotsData.Add(PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count, slot.Position.X, slot.Position.Y));
        }

        return slotsData.ToArray();
    }

    public static void Read(Storage storage, long[] slotsData, string[] paletteItems)
    {
        foreach (var slotData in slotsData)
        {
            PackingTools.UnpackShorts(slotData, out var paletteId, out var count, out var posX, out var posY);
            if (paletteId < 0 || paletteId >= paletteItems.Length) continue;

            var item = GameAssets.GetItemByFullID(paletteItems[paletteId]);
            if (item != null) storage.GetSlot(posX, posY)?.SetData(item, (byte)count);
        }
    }
}
