using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing
{
    public class NormalViewEffect : PostProcessEffect
    {
        private Shader shader;
        private readonly SceneRenderer renderer;
        public NormalViewEffect(Shader shader, SceneRenderer renderer)
        {
            this.renderer = renderer;
            this.shader = shader;
        }

        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
            shader.Use();

         
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderer.NormalTexture);
            shader.SetInt("uNormalMap", 0);
            shader.SetVector2("uResolution", new Vector2(clientSize.X, clientSize.Y));
        }
    }

    
}