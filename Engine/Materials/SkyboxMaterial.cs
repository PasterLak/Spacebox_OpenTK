namespace Engine
{
    public class SkyboxMaterial : TextureMaterial
    {
        public SkyboxMaterial(Texture2D texture) : base(texture, Resources.Load<Shader>("Resources/Shaders/skybox"))
        {
            RenderMode = RenderMode.Opaque;
            RenderFace = RenderFace.Back;
        }

    }
}
