using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests;

public class GenerationPoolsTests
{
    [Fact]
    public void PaddedBlocks_HaveChunkPaddedDimensions()
    {
        var array = PaddedArrayPools.Blocks.Rent();

        Assert.Equal(Chunk.Size + 2, array.GetLength(0));
        Assert.Equal(Chunk.Size + 2, array.GetLength(1));
        Assert.Equal(Chunk.Size + 2, array.GetLength(2));

        PaddedArrayPools.Blocks.Return(array);
    }

    [Fact]
    public void PaddedBlocks_ReturnClearsReferences()
    {
        var array = PaddedArrayPools.Blocks.Rent();
        array[0, 0, 0] = new Block();
        array[33, 33, 33] = new Block();

        PaddedArrayPools.Blocks.Return(array);

        Assert.Null(array[0, 0, 0]);
        Assert.Null(array[33, 33, 33]);
    }

    [Fact]
    public void MeshBuildBuffers_TrimShrinksOversizedBuffers()
    {
        var buffers = new MeshBuildBuffers();
        int initialVertices = buffers.Vertices.Length;
        int initialIndices = buffers.Indices.Length;

        buffers.Vertices = new float[MeshBuildBuffers.MaxRetainedFaces * 4 * BuffersData.FloatsPerVertexBlock + 1];
        buffers.Trim();

        Assert.Equal(initialVertices, buffers.Vertices.Length);
        Assert.Equal(initialIndices, buffers.Indices.Length);
    }

    [Fact]
    public void MeshBuildBuffers_TrimKeepsGrownBuffersWithinLimit()
    {
        var buffers = new MeshBuildBuffers();
        var grown = new float[buffers.Vertices.Length * 2];
        buffers.Vertices = grown;

        buffers.Trim();

        Assert.Same(grown, buffers.Vertices);
    }

    [Fact]
    public void LightScratch_ClearEmptiesAllCollections()
    {
        var scratch = new LightPropagationScratch();
        scratch.LightQueue.Enqueue(5);

        scratch.Clear();

        Assert.Empty(scratch.LightQueue);
        Assert.All(scratch.RegionChunks, Assert.Null);
        Assert.Empty(scratch.ChunkToIndex);
        Assert.Empty(scratch.IndexToChunk);
        Assert.Empty(scratch.ChangedChunks);
    }

    [Fact]
    public void CachedFaceVectors_MatchComputedForEveryCombination()
    {
        foreach (var baseFront in Enum.GetValues<Direction>())
        foreach (var facing in Enum.GetValues<Direction>())
        foreach (var rotation in Enum.GetValues<Rotation>())
        {
            var cached = BlockRotationHelper.GetCachedFaceVectors(baseFront, facing, rotation);
            var computed = BlockRotationHelper.GetTransformedFaceVectors(baseFront, facing, rotation);

            foreach (var pair in computed)
                Assert.InRange(Vector3.Distance(pair.Value, cached[pair.Key]), 0f, 0.00001f);
        }
    }

    [Fact]
    public void CachedFaceVectors_ReturnSameInstance()
    {
        var a = BlockRotationHelper.GetCachedFaceVectors(Direction.Forward, Direction.Up, Rotation.Half);
        var b = BlockRotationHelper.GetCachedFaceVectors(Direction.Forward, Direction.Up, Rotation.Half);

        Assert.Same(a, b);
    }
}
