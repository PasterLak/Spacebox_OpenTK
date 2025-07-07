namespace Engine
{
    public class SkyboxMaterial : TextureMaterial
    {
        public SkyboxMaterial(Texture2D texture) : base(texture, Resources.Load<Shader>("Shaders/skybox"))
        {
            RenderMode = RenderMode.Fade;
            RenderFace = RenderFace.Back;
        }

    }
}
