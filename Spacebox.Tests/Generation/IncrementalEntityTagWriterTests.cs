using System.Diagnostics;
using Engine;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Tests.Generation;

public class IncrementalEntityTagWriterTests
{
    private static Block Air() => new Block(0, Direction.Up, 0, 0, false, true, false, false);

    private static SpaceEntity CreateShip(int chunkCount)
    {
        var entity = new SpaceEntity(-42, new Vector3(10, 20, 30), null!);
        var chunks = new Chunk[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            var chunk = new Chunk(new Vector3SByte(i, 0, 0), entity, emptyChunk: true);
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
                chunk.Blocks[x, y, z] = Air();
            chunks[i] = chunk;
        }
        entity.AddChunks(chunks, false);
        return entity;
    }

    [Fact]
    public void StepByStep_WritesTheSameChunksAsTheOneShotSerializer()
    {
        var entity = CreateShip(3);
        var expected = NBTHelper.SpaceEntityToTag(entity, Vector3.Zero);

        var writer = new IncrementalEntityTagWriter(entity, Vector3.Zero);
        int steps = 0;
        while (!writer.Step(Stopwatch.GetTimestamp())) steps++;
        var actual = writer.Finish();

        Assert.True(steps >= 1);
        Assert.Equal(expected.Get<ListTag>(NBTKey.ENTITY.chunks).Count, actual.Get<ListTag>(NBTKey.ENTITY.chunks).Count);
        Assert.Equal(expected.Get<LongTag>(NBTKey.ENTITY.id).Value, actual.Get<LongTag>(NBTKey.ENTITY.id).Value);
        Assert.Equal(expected.Get<FloatTag>(NBTKey.ENTITY.local_x).Value, actual.Get<FloatTag>(NBTKey.ENTITY.local_x).Value);
    }

    [Fact]
    public void Finish_AddsChunksCreatedAfterTheWriterStarted()
    {
        var entity = CreateShip(1);
        var writer = new IncrementalEntityTagWriter(entity, Vector3.Zero);

        var late = new Chunk(new Vector3SByte(1, 0, 0), entity, emptyChunk: true);
        for (int x = 0; x < Chunk.Size; x++)
        for (int y = 0; y < Chunk.Size; y++)
        for (int z = 0; z < Chunk.Size; z++)
            late.Blocks[x, y, z] = Air();
        entity.AddChunks(new[] { late }, false);

        while (!writer.Step(long.MaxValue)) { }

        Assert.Equal(2, writer.Finish().Get<ListTag>(NBTKey.ENTITY.chunks).Count);
    }
}
