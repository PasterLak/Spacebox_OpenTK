using SharpNBT;

namespace Spacebox.Game.Generation.Blocks.Components;

public interface ISavedComponent
{
    bool Save(CompoundTag root, Dictionary<string, short> itemsPalette, ref short indexInPalette);
    void Load(CompoundTag tag, string[] paletteItems);
    void OnLoaded();
}
