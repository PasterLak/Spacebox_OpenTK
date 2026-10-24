namespace Engine.Graphics.Rhi;

public readonly record struct SamplerState(
    TextureFilter MinFilter,
    TextureFilter MagFilter,
    TextureWrap WrapS,
    TextureWrap WrapT,
    TextureWrap WrapR = TextureWrap.Repeat,
    int MaxLevel = -1)
{
    public static readonly SamplerState LinearRepeat = new(TextureFilter.Linear, TextureFilter.Linear, TextureWrap.Repeat, TextureWrap.Repeat);
    public static readonly SamplerState LinearClamp = new(TextureFilter.Linear, TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge);
    public static readonly SamplerState NearestRepeat = new(TextureFilter.Nearest, TextureFilter.Nearest, TextureWrap.Repeat, TextureWrap.Repeat);
    public static readonly SamplerState NearestClamp = new(TextureFilter.Nearest, TextureFilter.Nearest, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge);
    public static readonly SamplerState TrilinearRepeat = new(TextureFilter.LinearMipmapLinear, TextureFilter.Linear, TextureWrap.Repeat, TextureWrap.Repeat);

    public bool HasMaxLevel => MaxLevel >= 0;
}
