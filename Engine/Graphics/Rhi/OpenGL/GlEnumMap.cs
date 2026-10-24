using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics.Rhi.OpenGL;

internal static class GlEnumMap
{
    public static BufferTarget ToGl(BufferKind kind) => kind switch
    {
        BufferKind.Vertex => BufferTarget.ArrayBuffer,
        BufferKind.Index => BufferTarget.ElementArrayBuffer,
        BufferKind.Uniform => BufferTarget.UniformBuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static BufferRangeTarget ToGlRange(BufferKind kind) => kind switch
    {
        BufferKind.Uniform => BufferRangeTarget.UniformBuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static BufferUsageHint ToGl(BufferUsage usage) => usage switch
    {
        BufferUsage.Static => BufferUsageHint.StaticDraw,
        BufferUsage.Dynamic => BufferUsageHint.DynamicDraw,
        BufferUsage.Stream => BufferUsageHint.StreamDraw,
        _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
    };

    public static VertexAttribPointerType ToGl(VertexComponentType type) => type switch
    {
        VertexComponentType.Float => VertexAttribPointerType.Float,
        VertexComponentType.Byte => VertexAttribPointerType.Byte,
        VertexComponentType.UnsignedByte => VertexAttribPointerType.UnsignedByte,
        VertexComponentType.Short => VertexAttribPointerType.Short,
        VertexComponentType.UnsignedShort => VertexAttribPointerType.UnsignedShort,
        VertexComponentType.Int => VertexAttribPointerType.Int,
        VertexComponentType.UnsignedInt => VertexAttribPointerType.UnsignedInt,
        VertexComponentType.HalfFloat => VertexAttribPointerType.HalfFloat,
        VertexComponentType.Double => VertexAttribPointerType.Double,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static DrawElementsType ToGl(IndexType type) => type switch
    {
        IndexType.UnsignedShort => DrawElementsType.UnsignedShort,
        IndexType.UnsignedInt => DrawElementsType.UnsignedInt,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static PrimitiveType ToGl(PrimitiveTopology topology) => topology switch
    {
        PrimitiveTopology.Points => PrimitiveType.Points,
        PrimitiveTopology.Lines => PrimitiveType.Lines,
        PrimitiveTopology.LineStrip => PrimitiveType.LineStrip,
        PrimitiveTopology.LineLoop => PrimitiveType.LineLoop,
        PrimitiveTopology.Triangles => PrimitiveType.Triangles,
        PrimitiveTopology.TriangleStrip => PrimitiveType.TriangleStrip,
        PrimitiveTopology.TriangleFan => PrimitiveType.TriangleFan,
        _ => throw new ArgumentOutOfRangeException(nameof(topology), topology, null)
    };

    public static TextureTarget ToGl(TextureType type) => type switch
    {
        TextureType.Texture2D => TextureTarget.Texture2D,
        TextureType.Texture2DArray => TextureTarget.Texture2DArray,
        TextureType.TextureCube => TextureTarget.TextureCubeMap,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static TextureTarget ToGl(CubeFace face) => face switch
    {
        CubeFace.PositiveX => TextureTarget.TextureCubeMapPositiveX,
        CubeFace.NegativeX => TextureTarget.TextureCubeMapNegativeX,
        CubeFace.PositiveY => TextureTarget.TextureCubeMapPositiveY,
        CubeFace.NegativeY => TextureTarget.TextureCubeMapNegativeY,
        CubeFace.PositiveZ => TextureTarget.TextureCubeMapPositiveZ,
        CubeFace.NegativeZ => TextureTarget.TextureCubeMapNegativeZ,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
    };

    public static GlTextureFormat ToGl(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8 => new(PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
        TextureFormat.Rgb8 => new(PixelInternalFormat.Rgb8, PixelFormat.Rgb, PixelType.UnsignedByte),
        TextureFormat.Rg8 => new(PixelInternalFormat.Rg8, PixelFormat.Rg, PixelType.UnsignedByte),
        TextureFormat.R8 => new(PixelInternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
        TextureFormat.Rgba16F => new(PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float),
        TextureFormat.Rgb16F => new(PixelInternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.Float),
        TextureFormat.R16F => new(PixelInternalFormat.R16f, PixelFormat.Red, PixelType.Float),
        TextureFormat.Rgba32F => new(PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
        TextureFormat.R32F => new(PixelInternalFormat.R32f, PixelFormat.Red, PixelType.Float),
        TextureFormat.Depth24 => new(PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, PixelType.Float),
        TextureFormat.Depth32F => new(PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float),
        TextureFormat.Depth24Stencil8 => new(PixelInternalFormat.Depth24Stencil8, PixelFormat.DepthStencil, PixelType.UnsignedInt248),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static RenderbufferStorage ToGlRenderbuffer(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8 => RenderbufferStorage.Rgba8,
        TextureFormat.Rgb8 => RenderbufferStorage.Rgb8,
        TextureFormat.Rg8 => RenderbufferStorage.Rg8,
        TextureFormat.R8 => RenderbufferStorage.R8,
        TextureFormat.Rgba16F => RenderbufferStorage.Rgba16f,
        TextureFormat.Rgb16F => RenderbufferStorage.Rgb16f,
        TextureFormat.R16F => RenderbufferStorage.R16f,
        TextureFormat.Rgba32F => RenderbufferStorage.Rgba32f,
        TextureFormat.R32F => RenderbufferStorage.R32f,
        TextureFormat.Depth24 => RenderbufferStorage.DepthComponent24,
        TextureFormat.Depth32F => RenderbufferStorage.DepthComponent32f,
        TextureFormat.Depth24Stencil8 => RenderbufferStorage.Depth24Stencil8,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static int ToGl(TextureFilter filter) => filter switch
    {
        TextureFilter.Nearest => (int)TextureMinFilter.Nearest,
        TextureFilter.Linear => (int)TextureMinFilter.Linear,
        TextureFilter.NearestMipmapNearest => (int)TextureMinFilter.NearestMipmapNearest,
        TextureFilter.LinearMipmapNearest => (int)TextureMinFilter.LinearMipmapNearest,
        TextureFilter.NearestMipmapLinear => (int)TextureMinFilter.NearestMipmapLinear,
        TextureFilter.LinearMipmapLinear => (int)TextureMinFilter.LinearMipmapLinear,
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
    };

    public static int ToGlMag(TextureFilter filter) => filter switch
    {
        TextureFilter.Nearest or TextureFilter.NearestMipmapNearest or TextureFilter.NearestMipmapLinear => (int)TextureMagFilter.Nearest,
        TextureFilter.Linear or TextureFilter.LinearMipmapNearest or TextureFilter.LinearMipmapLinear => (int)TextureMagFilter.Linear,
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
    };

    public static int ToGl(TextureWrap wrap) => wrap switch
    {
        TextureWrap.Repeat => (int)TextureWrapMode.Repeat,
        TextureWrap.MirroredRepeat => (int)TextureWrapMode.MirroredRepeat,
        TextureWrap.ClampToEdge => (int)TextureWrapMode.ClampToEdge,
        TextureWrap.ClampToBorder => (int)TextureWrapMode.ClampToBorder,
        _ => throw new ArgumentOutOfRangeException(nameof(wrap), wrap, null)
    };

    public static BlendingFactor ToGl(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => BlendingFactor.Zero,
        BlendFactor.One => BlendingFactor.One,
        BlendFactor.SrcColor => BlendingFactor.SrcColor,
        BlendFactor.OneMinusSrcColor => BlendingFactor.OneMinusSrcColor,
        BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
        BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
        BlendFactor.DstColor => BlendingFactor.DstColor,
        BlendFactor.OneMinusDstColor => BlendingFactor.OneMinusDstColor,
        BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
        BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
        _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
    };

    public static CullFaceMode ToGl(CullFace face) => face switch
    {
        CullFace.Back => CullFaceMode.Back,
        CullFace.Front => CullFaceMode.Front,
        CullFace.FrontAndBack => CullFaceMode.FrontAndBack,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
    };

    public static DepthFunction ToGl(CompareFunction function) => function switch
    {
        CompareFunction.Never => DepthFunction.Never,
        CompareFunction.Less => DepthFunction.Less,
        CompareFunction.Equal => DepthFunction.Equal,
        CompareFunction.LessOrEqual => DepthFunction.Lequal,
        CompareFunction.Greater => DepthFunction.Greater,
        CompareFunction.NotEqual => DepthFunction.Notequal,
        CompareFunction.GreaterOrEqual => DepthFunction.Gequal,
        CompareFunction.Always => DepthFunction.Always,
        _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
    };

    public static PolygonMode ToGl(FillMode mode) => mode switch
    {
        FillMode.Solid => PolygonMode.Fill,
        FillMode.Wireframe => PolygonMode.Line,
        FillMode.Points => PolygonMode.Point,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    public static ClearBufferMask ToGl(ClearTargets targets)
    {
        ClearBufferMask mask = 0;
        if ((targets & ClearTargets.Color) != 0) mask |= ClearBufferMask.ColorBufferBit;
        if ((targets & ClearTargets.Depth) != 0) mask |= ClearBufferMask.DepthBufferBit;
        if ((targets & ClearTargets.Stencil) != 0) mask |= ClearBufferMask.StencilBufferBit;
        return mask;
    }

    public static FramebufferAttachment ToGl(AttachmentPoint attachment) => attachment switch
    {
        AttachmentPoint.Color0 => FramebufferAttachment.ColorAttachment0,
        AttachmentPoint.Color1 => FramebufferAttachment.ColorAttachment1,
        AttachmentPoint.Color2 => FramebufferAttachment.ColorAttachment2,
        AttachmentPoint.Color3 => FramebufferAttachment.ColorAttachment3,
        AttachmentPoint.Color4 => FramebufferAttachment.ColorAttachment4,
        AttachmentPoint.Color5 => FramebufferAttachment.ColorAttachment5,
        AttachmentPoint.Color6 => FramebufferAttachment.ColorAttachment6,
        AttachmentPoint.Color7 => FramebufferAttachment.ColorAttachment7,
        AttachmentPoint.Depth => FramebufferAttachment.DepthAttachment,
        AttachmentPoint.DepthStencil => FramebufferAttachment.DepthStencilAttachment,
        _ => throw new ArgumentOutOfRangeException(nameof(attachment), attachment, null)
    };

    public static ObjectLabelIdentifier ToGl(DebugObjectKind kind) => kind switch
    {
        DebugObjectKind.Buffer => ObjectLabelIdentifier.Buffer,
        DebugObjectKind.Shader => ObjectLabelIdentifier.Shader,
        DebugObjectKind.Program => ObjectLabelIdentifier.Program,
        DebugObjectKind.VertexArray => ObjectLabelIdentifier.VertexArray,
        DebugObjectKind.Query => ObjectLabelIdentifier.Query,
        DebugObjectKind.Texture => ObjectLabelIdentifier.Texture,
        DebugObjectKind.Renderbuffer => ObjectLabelIdentifier.Renderbuffer,
        DebugObjectKind.Framebuffer => ObjectLabelIdentifier.Framebuffer,
        DebugObjectKind.Sampler => ObjectLabelIdentifier.Sampler,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
