
using Engine;
using OpenTK.Mathematics;
namespace Spacebox;
public class AtmosphereMaterial : TextureMaterial
{
    public Vector3 GlowColor { get; set; } = new Vector3(1f, 0.8f, 0);
    public float GlowIntensity { get; set; } = 1f;
    public AtmosphereMaterial(Texture2D texture) : base(texture,
        Resources.Load<Shader>("Shaders/atmosphere"))
    {
        RenderMode = RenderMode.Fade;

        RenderFace = RenderFace.Both;
    }
    protected override void UpdateDynamicUniforms()
    {
        base.UpdateDynamicUniforms();

        Camera cam = Camera.Main;

        Shader.SetVector3("cameraPos", cam.Position);
        Shader.SetVector3("glowColor", GlowColor);
        Shader.SetFloat("glowIntensity", GlowIntensity);

    }

}

