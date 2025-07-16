namespace Engine
{
    public class TransparentMaterial : TextureMaterial
    {
        public TransparentMaterial(Texture2D texture) : base(texture,
            Resources.Load<Shader>("Shaders/transparent"))
        {
            RenderMode = RenderMode.Fade;
            RenderFace = RenderFace.Both;
        }
    }
}
