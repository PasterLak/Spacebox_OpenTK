using System.Runtime.InteropServices;

namespace Engine.Graphics.Rhi;

public static class GraphicsDeviceExtensions
{
    public static void BufferData<T>(this IGraphicsDevice device, BufferKind kind, BufferHandle buffer, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        device.BufferData(kind, buffer, MemoryMarshal.AsBytes(data), usage);
    }

    public static void BufferSubData<T>(this IGraphicsDevice device, BufferKind kind, BufferHandle buffer, int offsetInBytes, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        device.BufferSubData(kind, buffer, offsetInBytes, MemoryMarshal.AsBytes(data));
    }

    public static void SetTextureData2D<T>(this IGraphicsDevice device, TextureHandle texture, int level, TextureFormat format, int width, int height, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        device.SetTextureData2D(texture, level, format, width, height, MemoryMarshal.AsBytes(data));
    }

    public static void ReadTexture2D<T>(this IGraphicsDevice device, TextureHandle texture, int level, TextureFormat format, Span<T> destination)
        where T : unmanaged
    {
        device.ReadTexture2D(texture, level, format, MemoryMarshal.AsBytes(destination));
    }

    public static void BindTexture(this IGraphicsDevice device, int unit, TextureHandle texture)
    {
        device.BindTexture(unit, TextureType.Texture2D, texture);
    }

    public static void BindTexture(this IGraphicsDevice device, int unit, int texture)
    {
        device.BindTexture(unit, TextureType.Texture2D, new TextureHandle(texture));
    }

    public static void SetViewport(this IGraphicsDevice device, OpenTK.Mathematics.Box2i viewport)
    {
        device.SetViewport(viewport.Min.X, viewport.Min.Y, viewport.Size.X, viewport.Size.Y);
    }
}
