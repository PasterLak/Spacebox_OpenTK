using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class ParticleMaterial : MaterialBase
    {
        public Texture2D? MainTexture { get; set; }

        public ParticleMaterial(Texture2D texture) : base(Resources.Load<Shader>("Shaders/particle"))
        {
            MainTexture = texture;
            RenderFace = RenderFace.Both;
            RenderMode = RenderMode.Fade;
        }

        public ParticleMaterial(Texture2D texture, Shader shader) :
            base(shader)
        {
            MainTexture = texture;
            RenderFace = RenderFace.Both;
            RenderMode = RenderMode.Fade;
        }

        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            UseTexture(MainTexture, "particleTexture");
        }

    }
}
