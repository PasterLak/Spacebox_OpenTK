using OpenTK.Mathematics;
using Engine.Graphics.Rhi;
using Engine.Graphics.Transparency;

namespace Engine.Graphics;

public sealed class RenderTexture : IDisposable
{
    private FramebufferHandle _fbo;
    private RenderbufferHandle _depth;
    private TextureHandle _color;
    private bool _disposed;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int ColorTexture => _color.Id;
    public TextureHandle ColorTextureHandle => _color;
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

        var device = Gpu.Device;
        var previousFbo = device.GetBoundFramebuffer();
        var previousViewport = device.GetViewport();
        var previousCamera = Camera.Main;

        camera.AspectRatio = (float)Width / Height;
        Camera.Main = camera;
        RenderSpace.UpdateOrigin();
        GlobalUniforms.Push();

        try
        {
            device.BindFramebuffer(_fbo);
            device.SetViewport(0, 0, Width, Height);
            GLState.DepthMask(true);
            GLState.DepthTest(true);
            device.SetClearColor(ClearColor);
            device.Clear(ClearTargets.Color | ClearTargets.Depth);
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
            device.BindFramebuffer(previousFbo);
            device.SetViewport(previousViewport);
            Camera.Main = previousCamera;
            RenderSpace.UpdateOrigin();
            GlobalUniforms.Push();
        }
    }

    public byte[] ReadPixels()
    {
        var pixels = new byte[Width * Height * 4];
        var device = Gpu.Device;
        var previousFbo = device.GetBoundFramebuffer();
        device.BindFramebuffer(_fbo);
        device.ReadPixels(0, 0, Width, Height, TextureFormat.Rgba8, pixels);
        device.BindFramebuffer(previousFbo);
        return pixels;
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

        var device = Gpu.Device;
        _fbo = device.CreateFramebuffer();
        device.BindFramebuffer(_fbo);

        _color = device.CreateTexture(TextureType.Texture2D);
        device.SetTextureData2D(_color, 0, TextureFormat.Rgba8, width, height, ReadOnlySpan<byte>.Empty);
        device.SetSampler(_color, TextureType.Texture2D, SamplerState.LinearRepeat);
        device.AttachTexture(AttachmentPoint.Color0, _color, 0);

        _depth = device.CreateRenderbuffer(TextureFormat.Depth24, width, height);
        device.AttachRenderbuffer(AttachmentPoint.Depth, _depth);

        device.SetDrawBuffers(1);
        device.BindFramebuffer(FramebufferHandle.Default);
    }

    private void Delete()
    {
        var device = Gpu.Device;
        device.DeleteFramebuffer(_fbo);
        device.DeleteTexture(_color);
        device.DeleteRenderbuffer(_depth);
        _fbo = FramebufferHandle.Default;
        _depth = RenderbufferHandle.None;
        _color = TextureHandle.None;
    }
}
