using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;

namespace Spacebox.Tests.Game;

public class BlockVertexTests
{
    private const float ByteStep = 1f / 255f;

    [Fact]
    public void Write_UsesTheBlockBufferLayout()
    {
        var buffer = new float[BlockVertex.Words * 2];
        int offset = 0;
        BlockVertex.Write(buffer, ref offset, new Vector3(1, 2, 3), new Vector2(0.25f, 0.75f), Vector3.One, Vector3.UnitY, 1f, false);

        Assert.Equal(BlockVertex.Words, offset);
        Assert.Equal(BuffersData.FloatsPerVertexBlock, BlockVertex.Words);
        Assert.Equal(new[] { 1f, 2f, 3f, 0.25f, 0.75f }, buffer[..5]);
    }

    [Fact]
    public void Color_RoundTripsWithinOneStepAndKeepsEmissionExact()
    {
        var color = new Vector3(0.1f, 0.55f, 0.93f);
        var lit = BlockVertex.Unpack(BlockVertex.PackColor(color, true));
        var plain = BlockVertex.Unpack(BlockVertex.PackColor(color, false));

        Assert.InRange(MathF.Abs(lit.X - color.X), 0f, ByteStep);
        Assert.InRange(MathF.Abs(lit.Y - color.Y), 0f, ByteStep);
        Assert.InRange(MathF.Abs(lit.Z - color.Z), 0f, ByteStep);
        Assert.Equal(BlockVertex.EmissionFlag, (byte)MathF.Round(lit.W * 255f));
        Assert.Equal(0f, plain.W);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 3)]
    [InlineData(true, 2)]
    public void Flags_CarryEmissionAndDamageStage(bool emission, int stage)
    {
        byte flags = (byte)MathF.Round(BlockVertex.Unpack(BlockVertex.PackColor(Vector3.One, emission, stage)).W * 255f);

        Assert.Equal(emission, (flags & BlockVertex.EmissionFlag) != 0);
        Assert.Equal(stage, flags & BlockVertex.DamageMask);
    }

    [Theory]
    [InlineData(0f, 1f, 0f, 0.6f)]
    [InlineData(-1f, 0f, 0f, 1f)]
    [InlineData(0.577f, -0.577f, 0.577f, 0.25f)]
    public void Normal_RoundTripsAndCarriesAo(float x, float y, float z, float ao)
    {
        var packed = BlockVertex.Unpack(BlockVertex.PackNormal(new Vector3(x, y, z), ao));
        var normal = new Vector3(packed.X, packed.Y, packed.Z) * 2f - Vector3.One;

        Assert.InRange(MathF.Abs(normal.X - x), 0f, ByteStep * 2f);
        Assert.InRange(MathF.Abs(normal.Y - y), 0f, ByteStep * 2f);
        Assert.InRange(MathF.Abs(normal.Z - z), 0f, ByteStep * 2f);
        Assert.InRange(MathF.Abs(packed.W - ao), 0f, ByteStep);
    }

    [Fact]
    public void PackedWords_SurviveArrayCopies()
    {
        var buffer = new float[BlockVertex.Words];
        int offset = 0;
        BlockVertex.Write(buffer, ref offset, Vector3.Zero, Vector2.Zero, new Vector3(1f, 0f, 1f), -Vector3.UnitZ, 0.5f, true);

        var copy = new float[buffer.Length];
        Array.Copy(buffer, copy, buffer.Length);
        var list = new List<float>(copy);

        Assert.Equal(BitConverter.SingleToInt32Bits(buffer[5]), BitConverter.SingleToInt32Bits(list[5]));
        Assert.Equal(BitConverter.SingleToInt32Bits(buffer[6]), BitConverter.SingleToInt32Bits(list[6]));
    }
}
