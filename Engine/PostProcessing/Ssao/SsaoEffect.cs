using Engine.Graphics;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.Ssao;

public sealed class SsaoEffect : PostProcessEffect, IDisposable
{
    private const int NoiseSize = 4;

    private readonly SsaoShaders _shaders;
    private readonly SceneRenderer _renderer;
    private readonly SsaoTargets _targets = new SsaoTargets();
    private readonly Texture2D _noise;

    private SsaoSettings _settings = SsaoSettings.Default;
    private SsaoPreset _preset = SsaoPreset.For(SsaoSettings.Default.Quality);
    private int _blurRadius;
    private Vector3[] _kernel = SsaoKernel.Build(SsaoPreset.For(SsaoSettings.Default.Quality).SampleCount);
    private int _uploadedKernelHandle;
    private int _uploadedKernelLength;
    private bool _disposed;

    public override bool IsRequired => true;
    public bool DebugView { get; set; }
    public SsaoSettings Settings => _settings;

    public SsaoEffect(SsaoShaders shaders, SceneRenderer renderer)
    {
        _shaders = shaders ?? throw new ArgumentNullException(nameof(shaders));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _noise = SsaoNoise.GenerateRotationNoise(NoiseSize, SsaoKernel.DefaultSeed);
        Configure(SsaoSettings.Default);
    }

    public void Configure(SsaoSettings settings)
    {
        _settings = (settings ?? SsaoSettings.Default).Clamped();
        _preset = SsaoPreset.For(_settings.Quality);
        _blurRadius = _preset.BlurRadiusFor(_settings.Smoothness);
        Enabled = _settings.IsEnabled;

        if (_kernel.Length != _preset.SampleCount)
        {
            _kernel = SsaoKernel.Build(_preset.SampleCount);
            _uploadedKernelHandle = 0;
        }
    }

    public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
    {
        var camera = Camera.Main;
        if (camera == null)
        {
            BindComposite(inputTexture, false);
            return;
        }

        using (GPUDebug.Group("SSAO"))
        {
            Vector2i sceneSize = _renderer.Size;
            int divisor = _preset.ResolutionDivisor;
            _targets.EnsureSize(new Vector2i((sceneSize.X + divisor - 1) / divisor, (sceneSize.Y + divisor - 1) / divisor));

            GLState.Blend(false);
            GLState.DepthTest(false);

            Vector4 projMain = SsaoProjection.Pack(camera.GetProjectionMatrix());
            Vector4 projView = ViewModelProjection.Active ? SsaoProjection.Pack(ViewModelProjection.Projection) : projMain;

            var device = Gpu.Device;
            device.SetViewport(0, 0, _targets.Size.X, _targets.Size.Y);
            RenderDepth(sceneSize, projMain, projView);
            RenderOcclusion(sceneSize, projMain, projView);
            RenderBlur();

            device.BindFramebuffer(new FramebufferHandle(outputFbo));
            device.SetViewport(0, 0, clientSize.X, clientSize.Y);
            BindComposite(inputTexture, true);

            var composite = _shaders.Composite;
            composite.SetVector4("uProjMain", projMain);
            composite.SetVector4("uProjView", projView);
            composite.SetVector2("uSceneSize", sceneSize);
            composite.SetFloat("uStrength", _settings.Strength);
            composite.SetFloat("uViewModelStrength", _settings.ViewModelStrength);
            composite.SetFloat("uGrain", _settings.Grain);
            composite.SetInt("uGrainSize", _settings.GrainSize);
            composite.SetInt("uDebugView", DebugView || Engine.Graphics.Debugging.RenderDebugView.Mode == Engine.Graphics.Debugging.RenderDebugMode.Ssao ? 1 : 0);
            BindTexture(1, _targets.OcclusionTexture, composite, "uAo");
            BindTexture(2, _targets.DepthTexture, composite, "uAoDepth");
            BindTexture(3, _renderer.DepthTexture, composite, "uSceneDepth");
            BindTexture(4, _renderer.NormalTexture, composite, "uSceneNormal");
            device.BindTexture(0, inputTexture);
        }
    }

    private void RenderDepth(Vector2i sceneSize, Vector4 projMain, Vector4 projView)
    {
        var shader = _shaders.Depth;
        Gpu.Device.BindFramebuffer(_targets.DepthTarget);
        shader.Use();
        BindTexture(0, _renderer.DepthTexture, shader, "uSceneDepth");
        BindTexture(1, _renderer.NormalTexture, shader, "uSceneNormal");
        shader.SetVector4("uProjMain", projMain);
        shader.SetVector4("uProjView", projView);
        shader.SetVector2("uSceneSize", sceneSize);
        shader.SetInt("uDivisor", _preset.ResolutionDivisor);
        PostProcessManager.RenderQuad();
    }

    private void RenderOcclusion(Vector2i sceneSize, Vector4 projMain, Vector4 projView)
    {
        var shader = _shaders.Occlusion;
        Gpu.Device.BindFramebuffer(_targets.OcclusionTarget);
        shader.Use();
        UploadKernel(shader);
        BindTexture(0, _targets.DepthTexture, shader, "uDepth");
        BindTexture(1, _renderer.NormalTexture, shader, "uSceneNormal");
        BindTexture(2, _noise.Handle, shader, "uNoise");
        shader.SetInt("uSampleCount", _kernel.Length);
        shader.SetVector4("uProjMain", projMain);
        shader.SetVector4("uProjView", projView);
        shader.SetVector2("uSceneSize", sceneSize);
        shader.SetInt("uDivisor", _preset.ResolutionDivisor);
        shader.SetFloat("uRadius", _settings.Radius);
        shader.SetFloat("uViewModelRadius", _settings.ViewModelRadius);
        shader.SetFloat("uBias", _settings.Bias);
        shader.SetFloat("uFadeStart", _settings.FadeStart);
        shader.SetFloat("uFadeEnd", _settings.FadeEnd);
        PostProcessManager.RenderQuad();
    }

    private void RenderBlur()
    {
        if (_blurRadius <= 0) return;

        var shader = _shaders.Blur;
        shader.Use();
        shader.SetInt("uRadius", _blurRadius);
        BindTexture(1, _targets.DepthTexture, shader, "uDepth");

        Gpu.Device.BindFramebuffer(_targets.BlurTarget);
        BindTexture(0, _targets.OcclusionTexture, shader, "uAo");
        shader.SetVector2("uDirection", Vector2.UnitX);
        PostProcessManager.RenderQuad();

        Gpu.Device.BindFramebuffer(_targets.OcclusionTarget);
        BindTexture(0, _targets.BlurTexture, shader, "uAo");
        shader.SetVector2("uDirection", Vector2.UnitY);
        PostProcessManager.RenderQuad();
    }

    private void BindComposite(int inputTexture, bool active)
    {
        var composite = _shaders.Composite;
        composite.Use();
        BindTexture(0, inputTexture, composite, "uScene");
        composite.SetInt("uActive", active ? 1 : 0);
    }

    private void UploadKernel(Shader shader)
    {
        if (_uploadedKernelHandle == shader.Handle && _uploadedKernelLength == _kernel.Length) return;
        shader.SetVector3Array("uKernel", _kernel);
        _uploadedKernelHandle = shader.Handle;
        _uploadedKernelLength = _kernel.Length;
    }

    private static void BindTexture(int unit, int texture, Shader shader, string uniform)
    {
        Gpu.Device.BindTexture(unit, texture);
        shader.SetInt(uniform, unit);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _targets.Dispose();
        _noise.Dispose();
    }
}
