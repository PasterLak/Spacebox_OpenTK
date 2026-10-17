using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Doors;

namespace Spacebox.Game.Generation.Tools;

public class DoorSerializer : IBlockSerializer
{
    public string TagName => "door_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var door = (DoorBlock)block;
        if (door.HasMultiblockOffset || (!door.IsOpen && door.AutoCloseTimer <= 0f)) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new ByteTag("is_open", (byte)(door.IsOpen ? 1 : 0)));
        root.Add(new FloatTag("auto_close_timer", door.AutoCloseTimer));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var door = (DoorBlock)block;
        bool isOpen = tag.TryGetValue<ByteTag>("is_open", out var openTag) && openTag.Value == 1;
        float timer = tag.TryGetValue<FloatTag>("auto_close_timer", out var timerTag) ? timerTag.Value : 0f;
        door.Restore(isOpen, timer);
    }
}
