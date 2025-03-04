using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
namespace Engine.PostProcessing
{
    public class EdgeDetectionEffect : PostProcessEffect
    {
        private Shader edgeShader;
        public float EdgeThreshold { get; set; } = 0.1f;
        public EdgeDetectionEffect(Shader shader)
        {
            edgeShader = shader;
        
        }
        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, outputFbo);
            GL.Viewport(0, 0, clientSize.X, clientSize.Y);
            edgeShader.Use();
            edgeShader.SetInt("scene", 0);
            edgeShader.SetFloat("edgeThreshold", EdgeThreshold);
            edgeShader.SetVector2("texelSize", new Vector2(1.0f / clientSize.X, 1.0f / clientSize.Y));
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, inputTexture);
            PostProcessManager.RenderQuad();
        }
    }
}
