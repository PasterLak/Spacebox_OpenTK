using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class PictureFrameSerializer : IBlockSerializer
{
    public string TagName => "picture_frame_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var frame = (PictureFrameBlock)block;
        var picture = frame.Picture;
        if (picture == null) return null;

        if (itemsPalette.TryAdd(picture.Id_string, indexInPalette)) indexInPalette++;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new ShortTag("picture", itemsPalette[picture.Id_string]));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var frame = (PictureFrameBlock)block;
        if (!tag.TryGetValue<ShortTag>("picture", out var picture)) return;

        short index = picture.Value;
        if (index < 0 || index >= paletteItems.Length) return;

        frame.RestorePicture(GameAssets.GetItemByFullID(paletteItems[index]) as PictureItem);
    }
}
