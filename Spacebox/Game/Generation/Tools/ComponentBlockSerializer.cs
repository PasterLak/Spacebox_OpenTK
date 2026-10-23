using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game.Generation.Tools;

public class ComponentBlockSerializer : IBlockSerializer
{
    public string TagName => "component_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var host = (ComponentBlock)block;
        CompoundTag root = null;

        for (int i = 0; i < host.ComponentCount; i++)
        {
            if (host.GetComponent(i) is not ISavedComponent saved) continue;

            var candidate = root ?? CreateRoot(flatIndex);
            if (saved.Save(candidate, itemsPalette, ref indexInPalette)) root = candidate;
        }

        return root;
    }

    private CompoundTag CreateRoot(int flatIndex)
    {
        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var host = (ComponentBlock)block;

        host.IsLoading = true;
        try
        {
            for (int i = 0; i < host.ComponentCount; i++)
            {
                if (host.GetComponent(i) is ISavedComponent saved) saved.Load(tag, paletteItems);
            }
        }
        finally
        {
            host.IsLoading = false;
        }

        for (int i = 0; i < host.ComponentCount; i++)
        {
            if (host.GetComponent(i) is ISavedComponent saved) saved.OnLoaded();
        }
    }
}
