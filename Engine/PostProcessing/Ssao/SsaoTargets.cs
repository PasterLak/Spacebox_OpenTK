using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.Ssao;

public sealed class SsaoTargets : IDisposable
{
    public Vector2i Size { get; private set; }
    public int DepthFbo { get; private set; }
    public int DepthTexture { get; private set; }
    public int OcclusionFbo { get; private set; }
    public int OcclusionTexture { get; private set; }
    public int BlurFbo { get; private set; }
    public int BlurTexture { get; private set; }

    public void EnsureSize(Vector2i size)
    {
        size = new Vector2i(Math.Max(1, size.X), Math.Max(1, size.Y));
        if (size == Size && DepthFbo != 0) return;

        Release();
        Size = size;

        (DepthFbo, DepthTexture) = CreateTarget(PixelInternalFormat.R32f, PixelType.Float);
        (OcclusionFbo, OcclusionTexture) = CreateTarget(PixelInternalFormat.R8, PixelType.UnsignedByte);
        (BlurFbo, BlurTexture) = CreateTarget(PixelInternalFormat.R8, PixelType.UnsignedByte);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private (int fbo, int texture) CreateTarget(PixelInternalFormat format, PixelType type)
    {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, format, Size.X, Size.Y, 0, PixelFormat.Red, type, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

        int fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        return (fbo, texture);
    }

    private void Release()
    {
        DeleteTarget(DepthFbo, DepthTexture);
        DeleteTarget(OcclusionFbo, OcclusionTexture);
        DeleteTarget(BlurFbo, BlurTexture);
        DepthFbo = DepthTexture = OcclusionFbo = OcclusionTexture = BlurFbo = BlurTexture = 0;
        Size = Vector2i.Zero;
    }

    private static void DeleteTarget(int fbo, int texture)
    {
        if (fbo != 0) GL.DeleteFramebuffer(fbo);
        if (texture != 0) GL.DeleteTexture(texture);
    }

    public void Dispose()
    {
        Release();
    }
}
