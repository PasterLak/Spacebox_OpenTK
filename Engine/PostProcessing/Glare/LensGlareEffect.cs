using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing.Glare;

public sealed class LensGlareEffect : PostProcessEffect
{
    private readonly Shader _shader;
    private float _intensity;

    public override bool IsRequired => true;

    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = Math.Clamp(value, 0f, 1f);
            Enabled = _intensity > 0.001f;
        }
    }

    public Vector2 LightPosition { get; set; }
    public Vector3 Color { get; set; } = Vector3.One;

    public LensGlareEffect(Shader shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        Enabled = false;
    }

    public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
    {
        _shader.Use();
        _shader.SetInt("scene", 0);
        _shader.SetFloat("uIntensity", _intensity);
        _shader.SetVector2("uLightPos", LightPosition);
        _shader.SetVector3("uColor", Color);
        _shader.SetFloat("uAspect", clientSize.Y > 0 ? clientSize.X / (float)clientSize.Y : 1f);
        _shader.SetFloat("uTime", Time.Total);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, inputTexture);
    }
}
