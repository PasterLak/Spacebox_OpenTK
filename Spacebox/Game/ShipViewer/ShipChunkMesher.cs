using Engine;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Versioning;

namespace Spacebox.Game.ShipViewer;

public static class ShipChunkMesher
{
    public static Dictionary<Vector3SByte, Block[,,]> ReadChunks(CompoundTag entity, Func<bool> cancelled)
    {
        var chunks = new Dictionary<Vector3SByte, Block[,,]>();
        foreach (var chunkTag in ChunkTagAccess.Chunks(entity))
        {
            if (cancelled()) break;
            var blocks = NBTHelper.TagToChunkBlocks(chunkTag, out var index);
            chunks[index] = blocks;
        }
        return chunks;
    }

    public static (float[] Vertices, uint[] Indices) Mesh(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks, Vector3SByte index, Block air)
    {
        var padded = ShipChunkPadding.Build(chunks, index, air);
        try
        {
            var meshData = new MeshGenerator(index, padded, false).GenerateMeshData();
            try
            {
                if (meshData.VertexCount == 0 || meshData.IndexCount == 0) return (null, null);
                return (meshData.Vertices.AsSpan(0, meshData.VertexCount).ToArray(), meshData.Indices.AsSpan(0, meshData.IndexCount).ToArray());
            }
            finally
            {
                meshData.Release();
            }
        }
        finally
        {
            PaddedArrayPools.Blocks.Return(padded);
        }
    }
}
