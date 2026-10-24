namespace Engine.Graphics.Rhi;

public static class TextureFormatInfo
{
    public static int BytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8 => 4,
        TextureFormat.Rgb8 => 3,
        TextureFormat.Rg8 => 2,
        TextureFormat.R8 => 1,
        TextureFormat.Rgba16F => 16,
        TextureFormat.Rgb16F => 12,
        TextureFormat.R16F => 4,
        TextureFormat.Rgba32F => 16,
        TextureFormat.R32F => 4,
        TextureFormat.Depth24 => 4,
        TextureFormat.Depth32F => 4,
        TextureFormat.Depth24Stencil8 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static bool IsDepth(TextureFormat format) =>
        format is TextureFormat.Depth24 or TextureFormat.Depth32F or TextureFormat.Depth24Stencil8;

    public static int ByteSize(TextureFormat format, int width, int height, int layers = 1) =>
        BytesPerPixel(format) * width * height * layers;
}
