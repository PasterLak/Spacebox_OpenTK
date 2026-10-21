using Spacebox.Game;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests;

[Collection(GameAssetsCollection.Name)]
public class StorageSlotsNbtTests
{
    private static readonly Item Ore = new Item(121, 64, "ore") { Id_string = "nbttest:ore" };
    private static readonly Item Cell = new Item(122, 64, "cell") { Id_string = "nbttest:cell" };

    [Fact]
    public void WriteThenRead_RestoresItemsAndPositions()
    {
        GameAssets.AddItemString(Ore.Id_string, Ore);
        GameAssets.AddItemString(Cell.Id_string, Cell);
        try
        {
            var source = new Storage(4, 2);
            source.SetSlot(0, 0, Ore, 12);
            source.SetSlot(3, 1, Cell, 64);
            source.SetSlot(2, 0, Ore, 1);

            var palette = new Dictionary<string, short>();
            short nextIndex = 0;
            long[] data = StorageSlotsNbt.Write(source, palette, ref nextIndex);

            var paletteItems = new string[nextIndex];
            foreach (var pair in palette) paletteItems[pair.Value] = pair.Key;

            var restored = new Storage(4, 2);
            StorageSlotsNbt.Read(restored, data, paletteItems);

            Assert.Equal(3, data.Length);
            Assert.Equal(2, nextIndex);
            Assert.Equal(12, restored.GetSlot(0, 0).Count);
            Assert.Equal(1, restored.GetSlot(2, 0).Count);
            Assert.Equal(Cell.Id, restored.GetSlot(3, 1).Item.Id);
            Assert.Equal(64, restored.GetSlot(3, 1).Count);
            Assert.Equal(13, restored.GetTotalCountOf(Ore));
        }
        finally
        {
            GameAssets.ItemsStr.Remove(Ore.Id_string);
            GameAssets.ItemsStr.Remove(Cell.Id_string);
        }
    }

    [Fact]
    public void Read_SkipsUnknownItemsAndBadPaletteIndices()
    {
        var restored = new Storage(2, 1);
        long[] data =
        [
            PackingTools.PackShorts(0, 5, 0, 0),
            PackingTools.PackShorts(7, 5, 1, 0)
        ];

        StorageSlotsNbt.Read(restored, data, ["nbttest:missing"]);

        Assert.False(restored.HasAnyItems());
    }
}
