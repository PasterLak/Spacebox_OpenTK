using OpenTK.Mathematics;
using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class FlashlightGlareTests
{
    private const float Range = 50f;
    private static readonly float CosInner = MathF.Cos(MathHelper.DegreesToRadians(15f));
    private static readonly float CosOuter = MathF.Cos(MathHelper.DegreesToRadians(25f));

    private static float Strength(Vector3 eyePos, Vector3 eyeFront, Vector3 lightDir)
    {
        return FlashlightGlare.Strength(Vector3.Zero, lightDir, CosInner, CosOuter, Range, 1f, eyePos, eyeFront);
    }

    private static Vector3 AngleFromZ(float degrees)
    {
        float radians = MathHelper.DegreesToRadians(degrees);
        return new Vector3(MathF.Sin(radians), 0f, MathF.Cos(radians));
    }

    [Fact]
    public void LightInTheFace_AtCloseRange_IsStrong()
    {
        Assert.True(Strength(new Vector3(0, 0, 0.3f), -Vector3.UnitZ, Vector3.UnitZ) > 0.8f);
    }

    [Fact]
    public void Glare_EndsAtMaxDistance_EvenWithALongerFlashlight()
    {
        Assert.True(Strength(new Vector3(0, 0, FlashlightGlare.MaxDistance - 0.5f), -Vector3.UnitZ, Vector3.UnitZ) > 0f);
        Assert.Equal(0f, Strength(new Vector3(0, 0, FlashlightGlare.MaxDistance + 0.1f), -Vector3.UnitZ, Vector3.UnitZ));
    }

    [Fact]
    public void BeyondRange_GivesNoGlare()
    {
        Assert.Equal(0f, Strength(new Vector3(0, 0, FlashlightGlare.MaxDistance), -Vector3.UnitZ, Vector3.UnitZ));
        Assert.Equal(0f, Strength(new Vector3(0, 0, Range + 10f), -Vector3.UnitZ, Vector3.UnitZ));
    }

    [Fact]
    public void Glare_GrowsWhenTheLightComesCloser()
    {
        float far = Strength(new Vector3(0, 0, 4), -Vector3.UnitZ, Vector3.UnitZ);
        float near = Strength(new Vector3(0, 0, 1), -Vector3.UnitZ, Vector3.UnitZ);

        Assert.True(far > 0f);
        Assert.True(near > far);
    }

    [Fact]
    public void OutsideTheOuterCone_GivesNoGlare()
    {
        Assert.Equal(0f, Strength(new Vector3(0, 0, 2), -Vector3.UnitZ, AngleFromZ(24f)));
    }

    [Fact]
    public void BetweenInnerAndOuterCone_GivesPartialGlare()
    {
        float full = Strength(new Vector3(0, 0, 2), -Vector3.UnitZ, Vector3.UnitZ);
        float edge = Strength(new Vector3(0, 0, 2), -Vector3.UnitZ, AngleFromZ(16f));

        Assert.True(edge > 0f);
        Assert.True(edge < full);
    }

    [Fact]
    public void LookingAway_GivesNoGlare()
    {
        Assert.Equal(0f, Strength(new Vector3(0, 0, 2), Vector3.UnitZ, Vector3.UnitZ));
        Assert.Equal(0f, Strength(new Vector3(0, 0, 2), Vector3.UnitX, Vector3.UnitZ));
        Assert.Equal(0f, Strength(new Vector3(0, 0, 2), -AngleFromZ(30f), Vector3.UnitZ));
    }

    [Fact]
    public void ScreenPosition_StraightAhead_IsCenter()
    {
        bool visible = FlashlightGlare.TryGetScreenPosition(new Vector3(0, 0, -10), Vector3.Zero, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 80f, 16f / 9f, out var ndc);

        Assert.True(visible);
        Assert.Equal(0f, ndc.X, 3);
        Assert.Equal(0f, ndc.Y, 3);
    }

    [Fact]
    public void ScreenPosition_RightAndUp_HasPositiveCoordinates()
    {
        FlashlightGlare.TryGetScreenPosition(new Vector3(2, 1, -10), Vector3.Zero, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 80f, 16f / 9f, out var ndc);

        Assert.True(ndc.X > 0f);
        Assert.True(ndc.Y > 0f);
    }

    [Fact]
    public void ScreenPosition_BehindTheEye_IsNotVisible()
    {
        Assert.False(FlashlightGlare.TryGetScreenPosition(new Vector3(0, 0, 1), Vector3.Zero, -Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 80f, 16f / 9f, out _));
    }

    [Fact]
    public void Smooth_RisesFasterThanItFalls()
    {
        float rise = FlashlightGlare.Smooth(0f, 1f, 0.1f);
        float fall = 1f - FlashlightGlare.Smooth(1f, 0f, 0.1f);

        Assert.True(rise > fall);
        Assert.InRange(rise, 0f, 1f);
    }
}
