namespace Engine
{
    public class TransparentMaterial : TextureMaterial
    {
        public TransparentMaterial(Texture2D texture) : base(texture,
            ShaderManager.GetShader("Shaders/transparent"))
        {
            RenderMode = RenderMode.Fade;
            RenderFace = RenderFace.Both;
        }
    }
}
