using OpenTK.Mathematics;

namespace Engine;

public static class PixelSize
{
    public static Vector2i Resolve(Vector2i framebuffer, Vector2i client)
    {
        return framebuffer.X > 0 && framebuffer.Y > 0 ? framebuffer : client;
    }

    public static float Scale(Vector2i pixels, Vector2i client)
    {
        return client.X > 0 && pixels.X > 0 ? pixels.X / (float)client.X : 1f;
    }
}
