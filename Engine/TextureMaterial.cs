using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class TextureMaterial : MaterialBase
    {
        public Texture2D? MainTexture { get; set; }

        public TextureMaterial(Texture2D texture) : base(Resources.Load<Shader>("Shaders/textured"))
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Cutout;
        }

        public TextureMaterial(Texture2D texture, Shader shader) : 
            base(shader)
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Cutout;
        }

        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            UseTexture(MainTexture, "texture0");
        }

    }
}
