using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Engine.PostProcessing
{
    public class DefaultEffect : PostProcessEffect
    {
        private Shader blackWhiteShader;
        public DefaultEffect(Shader shader)
        {
            
            blackWhiteShader = shader;
        }
        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
        
            blackWhiteShader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, inputTexture);
            blackWhiteShader.SetInt("scene", 0);

           
        }
    }
}
