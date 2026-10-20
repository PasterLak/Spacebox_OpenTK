using OpenTK.Mathematics;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class FlashlightBeamShapeTests
{
    [Fact]
    public void Length_IsCappedForLongFlashlights()
    {
        Assert.Equal(FlashlightBeamShape.MaxLength, FlashlightBeamShape.Length(50f));
        Assert.Equal(6f, FlashlightBeamShape.Length(6f));
    }

    [Fact]
    public void Radius_FollowsTheOuterAngle()
    {
        float cos45 = MathF.Cos(MathHelper.DegreesToRadians(45f));

        Assert.Equal(10f, FlashlightBeamShape.Radius(10f, cos45), 3);
    }

    [Fact]
    public void Model_PutsTheApexAtTheStartAndTheRimAtTheEnd()
    {
        var model = FlashlightBeamShape.Model(new Vector3(1, 2, 3), Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, 2f, 10f);

        var apex = new Vector4(0, 0, 0, 1) * model;
        var rim = new Vector4(1, 0, 1, 1) * model;

        Assert.Equal(new Vector3(1, 2, 3), apex.Xyz);
        Assert.Equal(new Vector3(11, 4, 3), rim.Xyz);
    }

    [Fact]
    public void BeamBehindTheCamera_IsSkipped()
    {
        Assert.False(FlashlightBeamShape.IsVisible(new Vector3(0, 0, 20), Vector3.UnitZ, 10f, 2f, Vector3.Zero, -Vector3.UnitZ));
        Assert.True(FlashlightBeamShape.IsVisible(new Vector3(0, 0, -20), -Vector3.UnitZ, 10f, 2f, Vector3.Zero, -Vector3.UnitZ));
    }

    [Fact]
    public void BeamPointingAtTheCamera_FromBehind_IsStillVisibleWhenItReachesIt()
    {
        Assert.True(FlashlightBeamShape.IsVisible(new Vector3(0, 0, 3), -Vector3.UnitZ, 10f, 2f, Vector3.Zero, -Vector3.UnitZ));
    }

    [Fact]
    public void FarBeam_IsSkipped()
    {
        Assert.False(FlashlightBeamShape.IsVisible(new Vector3(0, 0, -200), -Vector3.UnitZ, 10f, 2f, Vector3.Zero, -Vector3.UnitZ));
    }

    [Fact]
    public void ViewFade_IsFullNearAndZeroBeyondMaxDistance()
    {
        Assert.Equal(1f, FlashlightBeamShape.ViewFade(new Vector3(0, 0, 10), Vector3.Zero), 3);
        Assert.Equal(0f, FlashlightBeamShape.ViewFade(new Vector3(0, 0, FlashlightBeamShape.MaxViewDistance), Vector3.Zero), 3);
        float middle = FlashlightBeamShape.ViewFade(new Vector3(0, 0, 25), Vector3.Zero);
        Assert.InRange(middle, 0.01f, 0.99f);
    }

    [Fact]
    public void Inside_IsOneOnTheAxisAndZeroOutside()
    {
        Assert.Equal(1f, FlashlightBeamShape.Inside(Vector3.Zero, Vector3.UnitZ, 10f, 2f, new Vector3(0, 0, 5)), 3);
        Assert.Equal(0f, FlashlightBeamShape.Inside(Vector3.Zero, Vector3.UnitZ, 10f, 2f, new Vector3(3, 0, 5)), 3);
        Assert.Equal(0f, FlashlightBeamShape.Inside(Vector3.Zero, Vector3.UnitZ, 10f, 2f, new Vector3(0, 0, -1)), 3);
        Assert.Equal(0f, FlashlightBeamShape.Inside(Vector3.Zero, Vector3.UnitZ, 10f, 2f, new Vector3(0, 0, 12)), 3);
    }

    [Fact]
    public void Mesh_HasMatchingVerticesAndIndices()
    {
        var vertices = FlashlightBeamMesh.BuildVertices();
        var indices = FlashlightBeamMesh.BuildIndices();
        int vertexCount = vertices.Length / 8;

        Assert.Equal((FlashlightBeamMesh.Segments + 1) * (FlashlightBeamMesh.Rings + 1), vertexCount);
        Assert.All(indices, i => Assert.InRange(i, 0, vertexCount - 1));
        Assert.Equal(0f, vertices[0], 5);
        Assert.Equal(0f, vertices[2], 5);
    }
}
