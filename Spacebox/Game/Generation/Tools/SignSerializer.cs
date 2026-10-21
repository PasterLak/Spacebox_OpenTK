using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class SignSerializer : IBlockSerializer
{
    public string TagName => "sign_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var sign = (SignBlock)block;
        if (sign.IsDefaultText) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new StringTag("text", sign.Text));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var sign = (SignBlock)block;
        if (tag.TryGetValue<StringTag>("text", out var text)) sign.RestoreText(text.Value);
    }
}
