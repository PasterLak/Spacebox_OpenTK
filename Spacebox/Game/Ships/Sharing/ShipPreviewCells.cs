using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.ShipViewer;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipPreviewCells
{
    public static IReadOnlyList<ShipPreviewCell> Read(CompoundTag entity, Func<string, int> kindOf)
    {
        var cells = new List<ShipPreviewCell>();
        foreach (var chunk in ChunkTagAccess.Chunks(entity))
        {
            var origin = ShipTagReader.ReadChunkIndex(chunk) * Generation.Chunk.Size;
            var palette = ChunkTagAccess.BlockPalette(chunk);
            var kinds = new int[palette.Count];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = ShipTagReader.IsEmpty(palette[i]) ? -1 : kindOf(palette[i]);

            var indices = ChunkBlockIndices.Read(chunk);
            for (int i = 0; i < indices.Length; i++)
            {
                int paletteIndex = indices[i];
                if (paletteIndex < 0 || paletteIndex >= kinds.Length || kinds[paletteIndex] < 0) continue;
                cells.Add(new ShipPreviewCell(origin + ShipTagReader.CellOf(i), kinds[paletteIndex]));
            }
        }
        return cells;
    }
}
