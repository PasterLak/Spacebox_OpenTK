using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Tools
{
    public interface IBlockSerializer
    {
        string TagName { get; }
        CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette);
        void Deserialize(Block block, CompoundTag tag, string[] paletteItems);
    }
}