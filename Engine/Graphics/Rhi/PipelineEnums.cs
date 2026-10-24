namespace Engine.Graphics.Rhi;

public enum BlendFactor : byte
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstColor,
    OneMinusDstColor,
    DstAlpha,
    OneMinusDstAlpha
}

public enum CullFace : byte
{
    Back,
    Front,
    FrontAndBack
}

public enum CompareFunction : byte
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always
}

public enum FillMode : byte
{
    Solid,
    Wireframe,
    Points
}

[Flags]
public enum ClearTargets : byte
{
    None = 0,
    Color = 1,
    Depth = 2,
    Stencil = 4
}

public enum AttachmentPoint : byte
{
    Color0,
    Color1,
    Color2,
    Color3,
    Color4,
    Color5,
    Color6,
    Color7,
    Depth,
    DepthStencil
}

public enum DebugObjectKind : byte
{
    Buffer,
    Shader,
    Program,
    VertexArray,
    Query,
    Texture,
    Renderbuffer,
    Framebuffer,
    Sampler
}
