using OpenTK.Mathematics;
using Spacebox.Engine;
using Spacebox.Engine.Light;

public class DirectionalLight : Light
{
    private Vector3 _ambient = new Vector3(0.05f);
    private Vector3 _diffuse = new Vector3(0.4f);
    private Vector3 _specular = new Vector3(0.5f);
    public override Vector3 Ambient
    {
        get => _ambient;
        set => _ambient = value;
    }
    public override Vector3 Diffuse
    {
        get => _diffuse;
        set => _diffuse = value;
    }
    public override Vector3 Specular
    {
        get => _specular;
        set => _specular = value;
    }

    public Vector3 Direction { get; set; } = new Vector3(-0.2f, -1.0f, -0.3f);

    public DirectionalLight(Shader shader) : base(shader) { }
    public DirectionalLight(Shader shader, Vector3 direction) : base(shader)
    {
        Direction = direction;
    }

    public override void Draw(Camera camera)
    {
        base.Draw(camera);
        Shader.SetVector3("dirLight.direction", Direction);
        Shader.SetVector3("dirLight.ambient", Ambient);
        Shader.SetVector3("dirLight.diffuse", Diffuse);
        Shader.SetVector3("dirLight.specular", Specular);
        Shader.SetFloat("spotLight.constant", 1.0f);
        Shader.SetFloat("spotLight.linear", 0.09f);
        Shader.SetFloat("spotLight.quadratic", 0.032f);
        Shader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
        Shader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
    }
}