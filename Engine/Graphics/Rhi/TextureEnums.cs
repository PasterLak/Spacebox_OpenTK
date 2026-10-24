namespace Engine.Graphics.Rhi;

public enum TextureType : byte
{
    Texture2D,
    Texture2DArray,
    TextureCube
}

public enum TextureFormat : byte
{
    Rgba8,
    Rgb8,
    Rg8,
    R8,
    Rgba16F,
    Rgb16F,
    R16F,
    Rgba32F,
    R32F,
    Depth24,
    Depth32F,
    Depth24Stencil8
}

public enum TextureFilter : byte
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear
}

public enum TextureWrap : byte
{
    Repeat,
    MirroredRepeat,
    ClampToEdge,
    ClampToBorder
}

public enum CubeFace : byte
{
    PositiveX,
    NegativeX,
    PositiveY,
    NegativeY,
    PositiveZ,
    NegativeZ
}
