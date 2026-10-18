using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Ships;

namespace Spacebox.Game.Generation.Tools;

public class ShipConsoleSerializer : IBlockSerializer
{
    public string TagName => "ship_console_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var console = (ShipConsoleBlock)block;
        var bindings = console.Entity?.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new ByteArrayTag("bindings", ConsoleBindingsCodec.ToBytes(bindings)));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        if (!tag.TryGetValue<ByteArrayTag>("bindings", out var data)) return;

        var console = (ShipConsoleBlock)block;
        console.PendingBindings = ConsoleBindingsCodec.FromBytes(data.ToArray());
    }
}
