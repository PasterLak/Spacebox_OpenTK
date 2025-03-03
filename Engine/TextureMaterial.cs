using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class TextureMaterial : MaterialBase
    {
        public Texture2D? MainTexture { get; set; }

        public TextureMaterial(Texture2D texture) : base(ShaderManager.GetShader("Shaders/textured"))
        {
            MainTexture = texture;
            RenderFace = RenderFace.Front;
            RenderMode = RenderMode.Opaque;
        }

        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            if (MainTexture != null)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                MainTexture.Use();
                Shader.SetInt("texture0", 0);
            }
        }

    }
}
