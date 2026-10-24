using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.Ssao;

public sealed class SsaoTargets : IDisposable
{
    private static readonly SamplerState TargetSampler = SamplerState.NearestClamp with { MaxLevel = 0 };

    private FramebufferHandle _depthFbo;
    private TextureHandle _depthTexture;
    private FramebufferHandle _occlusionFbo;
    private TextureHandle _occlusionTexture;
    private FramebufferHandle _blurFbo;
    private TextureHandle _blurTexture;

    public Vector2i Size { get; private set; }
    public int DepthFbo => _depthFbo.Id;
    public int DepthTexture => _depthTexture.Id;
    public int OcclusionFbo => _occlusionFbo.Id;
    public int OcclusionTexture => _occlusionTexture.Id;
    public int BlurFbo => _blurFbo.Id;
    public int BlurTexture => _blurTexture.Id;
    public FramebufferHandle DepthTarget => _depthFbo;
    public FramebufferHandle OcclusionTarget => _occlusionFbo;
    public FramebufferHandle BlurTarget => _blurFbo;

    public void EnsureSize(Vector2i size)
    {
        size = new Vector2i(Math.Max(1, size.X), Math.Max(1, size.Y));
        if (size == Size && !_depthFbo.IsDefault) return;

        Release();
        Size = size;

        var device = Gpu.Device;
        (_depthFbo, _depthTexture) = CreateTarget(device, TextureFormat.R32F);
        (_occlusionFbo, _occlusionTexture) = CreateTarget(device, TextureFormat.R8);
        (_blurFbo, _blurTexture) = CreateTarget(device, TextureFormat.R8);

        device.BindFramebuffer(FramebufferHandle.Default);
    }

    private (FramebufferHandle fbo, TextureHandle texture) CreateTarget(IGraphicsDevice device, TextureFormat format)
    {
        var texture = device.CreateTexture(TextureType.Texture2D);
        device.SetTextureData2D(texture, 0, format, Size.X, Size.Y, ReadOnlySpan<byte>.Empty);
        device.SetSampler(texture, TextureType.Texture2D, TargetSampler);

        var fbo = device.CreateFramebuffer();
        device.BindFramebuffer(fbo);
        device.AttachTexture(AttachmentPoint.Color0, texture, 0);
        device.SetDrawBuffers(1);
        return (fbo, texture);
    }

    private void Release()
    {
        var device = Gpu.Device;
        DeleteTarget(device, _depthFbo, _depthTexture);
        DeleteTarget(device, _occlusionFbo, _occlusionTexture);
        DeleteTarget(device, _blurFbo, _blurTexture);
        _depthFbo = _occlusionFbo = _blurFbo = FramebufferHandle.Default;
        _depthTexture = _occlusionTexture = _blurTexture = TextureHandle.None;
        Size = Vector2i.Zero;
    }

    private static void DeleteTarget(IGraphicsDevice device, FramebufferHandle fbo, TextureHandle texture)
    {
        device.DeleteFramebuffer(fbo);
        device.DeleteTexture(texture);
    }

    public void Dispose()
    {
        Release();
    }
}
