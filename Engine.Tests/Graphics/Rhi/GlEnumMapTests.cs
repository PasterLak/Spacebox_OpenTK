using Engine.Graphics.Rhi;
using Engine.Graphics.Rhi.OpenGL;

namespace Engine.Tests.Graphics.Rhi;

public sealed class GlEnumMapTests
{
    private static void AssertMapsEveryValue<TEnum, TResult>(Func<TEnum, TResult> map, bool distinct = true) where TEnum : struct, Enum
    {
        var results = new List<TResult>();
        foreach (var value in Enum.GetValues<TEnum>())
            results.Add(map(value));

        if (distinct) Assert.Equal(results.Count, results.Distinct().Count());
    }

    [Fact]
    public void BufferEnums_MapCompletely()
    {
        AssertMapsEveryValue<BufferKind, OpenTK.Graphics.OpenGL4.BufferTarget>(GlEnumMap.ToGl);
        AssertMapsEveryValue<BufferUsage, OpenTK.Graphics.OpenGL4.BufferUsageHint>(GlEnumMap.ToGl);
        AssertMapsEveryValue<VertexComponentType, OpenTK.Graphics.OpenGL4.VertexAttribPointerType>(GlEnumMap.ToGl);
        AssertMapsEveryValue<IndexType, OpenTK.Graphics.OpenGL4.DrawElementsType>(GlEnumMap.ToGl);
        AssertMapsEveryValue<PrimitiveTopology, OpenTK.Graphics.OpenGL4.PrimitiveType>(GlEnumMap.ToGl);
    }

    [Fact]
    public void TextureEnums_MapCompletely()
    {
        AssertMapsEveryValue<TextureType, OpenTK.Graphics.OpenGL4.TextureTarget>(GlEnumMap.ToGl);
        AssertMapsEveryValue<CubeFace, OpenTK.Graphics.OpenGL4.TextureTarget>(GlEnumMap.ToGl);
        AssertMapsEveryValue<TextureFormat, GlTextureFormat>(GlEnumMap.ToGl);
        AssertMapsEveryValue<TextureFormat, OpenTK.Graphics.OpenGL4.RenderbufferStorage>(GlEnumMap.ToGlRenderbuffer);
        AssertMapsEveryValue<TextureFilter, int>(GlEnumMap.ToGl);
        AssertMapsEveryValue<TextureFilter, int>(GlEnumMap.ToGlMag, distinct: false);
        AssertMapsEveryValue<TextureWrap, int>(GlEnumMap.ToGl);
    }

    [Fact]
    public void PipelineEnums_MapCompletely()
    {
        AssertMapsEveryValue<BlendFactor, OpenTK.Graphics.OpenGL4.BlendingFactor>(GlEnumMap.ToGl);
        AssertMapsEveryValue<CullFace, OpenTK.Graphics.OpenGL4.CullFaceMode>(GlEnumMap.ToGl);
        AssertMapsEveryValue<CompareFunction, OpenTK.Graphics.OpenGL4.DepthFunction>(GlEnumMap.ToGl);
        AssertMapsEveryValue<FillMode, OpenTK.Graphics.OpenGL4.PolygonMode>(GlEnumMap.ToGl);
        AssertMapsEveryValue<AttachmentPoint, OpenTK.Graphics.OpenGL4.FramebufferAttachment>(GlEnumMap.ToGl);
        AssertMapsEveryValue<DebugObjectKind, OpenTK.Graphics.OpenGL4.ObjectLabelIdentifier>(GlEnumMap.ToGl);
    }

    [Fact]
    public void ClearTargets_CombineIntoOneMask()
    {
        var mask = GlEnumMap.ToGl(ClearTargets.Color | ClearTargets.Depth);

        Assert.Equal(OpenTK.Graphics.OpenGL4.ClearBufferMask.ColorBufferBit | OpenTK.Graphics.OpenGL4.ClearBufferMask.DepthBufferBit, mask);
        Assert.Equal((OpenTK.Graphics.OpenGL4.ClearBufferMask)0, GlEnumMap.ToGl(ClearTargets.None));
    }

    [Fact]
    public void FloatFormats_UploadFloatData()
    {
        foreach (var format in Enum.GetValues<TextureFormat>())
        {
            var gl = GlEnumMap.ToGl(format);
            bool isFloat = gl.Type == OpenTK.Graphics.OpenGL4.PixelType.Float;
            int bytesPerComponent = isFloat ? 4 : 1;
            if (format == TextureFormat.Depth24Stencil8) continue;

            Assert.Equal(0, TextureFormatInfo.BytesPerPixel(format) % bytesPerComponent);
        }
    }

    [Fact]
    public void EveryTextureFormat_HasAPixelSize()
    {
        foreach (var format in Enum.GetValues<TextureFormat>())
            Assert.True(TextureFormatInfo.BytesPerPixel(format) > 0);

        Assert.True(TextureFormatInfo.IsDepth(TextureFormat.Depth24));
        Assert.False(TextureFormatInfo.IsDepth(TextureFormat.Rgba8));
        Assert.Equal(64, TextureFormatInfo.ByteSize(TextureFormat.Rgba8, 4, 4));
    }
}
