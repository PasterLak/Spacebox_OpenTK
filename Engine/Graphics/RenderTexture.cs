using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Graphics.Transparency;

namespace Engine.Graphics;

public sealed class RenderTexture : IDisposable
{
    private readonly int[] _viewport = new int[4];
    private int _fbo;
    private int _depth;
    private bool _disposed;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int ColorTexture { get; private set; }
    public Color4 ClearColor { get; set; } = new Color4(0.08f, 0.08f, 0.1f, 1f);

    public RenderTexture(int width, int height)
    {
        Create(Math.Max(1, width), Math.Max(1, height));
    }

    public void Resize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (width == Width && height == Height) return;

        Delete();
        Create(width, height);
    }

    public void Render(Camera camera, Action draw)
    {
        if (_disposed || camera == null || draw == null) return;

        int previousFbo = GL.GetInteger(GetPName.FramebufferBinding);
        GL.GetInteger(GetPName.Viewport, _viewport);
        var previousCamera = Camera.Main;

        camera.AspectRatio = (float)Width / Height;
        Camera.Main = camera;
        RenderSpace.UpdateOrigin();
        GlobalUniforms.Push();

        try
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            GL.Viewport(0, 0, Width, Height);
            GLState.DepthMask(true);
            GLState.DepthTest(true);
            GL.ClearColor(ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            TransparentQueue.BeginScope();
            try
            {
                draw();
            }
            finally
            {
                TransparentQueue.EndScope();
            }
        }
        finally
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFbo);
            GL.Viewport(_viewport[0], _viewport[1], _viewport[2], _viewport[3]);
            Camera.Main = previousCamera;
            RenderSpace.UpdateOrigin();
            GlobalUniforms.Push();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Delete();
        _disposed = true;
    }

    private void Create(int width, int height)
    {
        Width = width;
        Height = height;

        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        ColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);

        _depth = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depth);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depth);

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private void Delete()
    {
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteTexture(ColorTexture);
        GL.DeleteRenderbuffer(_depth);
        _fbo = 0;
        _depth = 0;
        ColorTexture = 0;
    }
}
