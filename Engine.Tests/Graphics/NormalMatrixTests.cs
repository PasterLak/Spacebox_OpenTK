using Engine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

public class NormalMatrixTests
{
    [Fact]
    public void MatchesTransposedInverseOfTheModel()
    {
        var model = Matrix4.CreateScale(2f, 0.5f, 3f) * Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(0.3f, -1.1f, 0.7f)) * Matrix4.CreateTranslation(10f, -4f, 7f);
        var expected = new Matrix3(Matrix4.Transpose(Matrix4.Invert(model)));
        var actual = NormalMatrix.From(model);

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                Assert.Equal(expected[r, c], actual[r, c], 4);
    }

    [Fact]
    public void SingularModel_UsesIdentity()
    {
        Assert.Equal(Matrix3.Identity, NormalMatrix.From(Matrix4.CreateScale(0f)));
    }

    [Theory]
    [InlineData(VertexAttribPointerType.UnsignedByte, 4, 4)]
    [InlineData(VertexAttribPointerType.Float, 3, 12)]
    [InlineData(VertexAttribPointerType.UnsignedShort, 2, 4)]
    public void BufferAttribute_ByteSizeFollowsComponentType(VertexAttribPointerType type, byte size, int expected)
    {
        Assert.Equal(expected, new BufferAttribute("a", size) { Type = type }.ByteSize);
    }

    [Fact]
    public void BufferAttribute_DefaultsToFloat()
    {
        Assert.Equal(VertexAttribPointerType.Float, new BufferAttribute { Name = "a", Size = 2 }.ComponentType);
    }
}
