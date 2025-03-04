using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.PostProcessing
{
    public class VignetteEffect : PostProcessEffect
    {
        private Shader vignetteShader;
        public float VignetteStrength { get; set; } = 0.5f;
        public float VignetteRadius { get; set; } = 0.75f;
        public float VignetteSoftness { get; set; } = 0.3f;
        public VignetteEffect(Shader shader)
        {
            vignetteShader = shader;
          
        }
        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
         
            vignetteShader.Use();
            vignetteShader.SetInt("scene", 0);
            vignetteShader.SetFloat("vignetteStrength", VignetteStrength);
            vignetteShader.SetFloat("vignetteRadius", VignetteRadius);
            vignetteShader.SetFloat("vignetteSoftness", VignetteSoftness);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, inputTexture);
          
        }
    }
}
