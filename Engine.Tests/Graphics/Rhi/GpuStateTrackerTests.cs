using Engine.Graphics.Rhi;

namespace Engine.Tests.Graphics.Rhi;

public sealed class GpuStateTrackerTests
{
    [Fact]
    public void Defaults_MatchTheStateTheDeviceResetsTo()
    {
        var state = new GpuStateTracker();

        Assert.True(state.DepthTest);
        Assert.True(state.DepthWrite);
        Assert.False(state.Blend);
        Assert.False(state.Culling);
        Assert.Equal(CullFace.Back, state.CullFace);
        Assert.Equal(0, state.ActiveUnit);
        Assert.Equal(0, state.Program);
    }

    [Fact]
    public void RedundantChanges_AreSkipped()
    {
        var state = new GpuStateTracker();

        Assert.False(state.SetDepthTest(true));
        Assert.True(state.SetDepthTest(false));
        Assert.False(state.SetDepthTest(false));
        Assert.True(state.SetBlend(true));
        Assert.False(state.SetBlend(true));
        Assert.True(state.SetCulling(true));
        Assert.False(state.SetCullFace(CullFace.Back));
        Assert.True(state.SetCullFace(CullFace.Front));
        Assert.False(state.SetBlendFunction(BlendFactor.One, BlendFactor.Zero));
        Assert.True(state.SetBlendFunction(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha));
        Assert.False(state.SetBlendFunction(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha));
        Assert.False(state.SetColorWrite(1, true));
        Assert.True(state.SetColorWrite(1, false));
        Assert.False(state.SetColorWrite(1, false));
    }

    [Fact]
    public void InvalidatedBlendFunction_IsAppliedAgain()
    {
        var state = new GpuStateTracker();
        state.SetBlendFunction(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);

        state.InvalidateBlendFunc();

        Assert.True(state.SetBlendFunction(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha));
        Assert.False(state.SetBlendFunction(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha));
    }

    [Fact]
    public void TextureBindings_AreCachedPerUnitAndType()
    {
        var state = new GpuStateTracker();

        Assert.True(state.SetTexture(0, TextureType.Texture2D, 5));
        Assert.False(state.SetTexture(0, TextureType.Texture2D, 5));
        Assert.True(state.SetTexture(1, TextureType.Texture2D, 5));
        Assert.True(state.SetTexture(0, TextureType.TextureCube, 5));
        Assert.Equal(5, state.GetTexture(0, TextureType.Texture2D));
    }

    [Fact]
    public void DeletedTexture_IsForgottenOnEveryUnit()
    {
        var state = new GpuStateTracker();
        state.SetTexture(0, TextureType.Texture2D, 7);
        state.SetTexture(3, TextureType.Texture2D, 7);

        state.ForgetTexture(7);

        Assert.Equal(0, state.GetTexture(0, TextureType.Texture2D));
        Assert.Equal(0, state.GetTexture(3, TextureType.Texture2D));
        Assert.True(state.SetTexture(3, TextureType.Texture2D, 7));
    }

    [Fact]
    public void InvalidatedTextures_RebindAndReactivateTheUnit()
    {
        var state = new GpuStateTracker();
        state.SetActiveUnit(0);
        state.SetTexture(0, TextureType.Texture2D, 2);

        state.InvalidateTextures();

        Assert.True(state.SetActiveUnit(0));
        Assert.True(state.SetTexture(0, TextureType.Texture2D, 2));
    }

    [Fact]
    public void Program_IsCachedUntilInvalidatedOrDeleted()
    {
        var state = new GpuStateTracker();

        Assert.True(state.SetProgram(4));
        Assert.False(state.SetProgram(4));
        state.InvalidateProgram();
        Assert.True(state.SetProgram(4));
        state.ForgetProgram(4);
        Assert.True(state.SetProgram(4));
    }

    [Fact]
    public void UnitOutsideTheCache_Throws()
    {
        var state = new GpuStateTracker();

        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetTexture(GpuStateTracker.MaxTextureUnits, TextureType.Texture2D, 1));
    }
}
