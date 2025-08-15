using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Engine.PostProcessing
{
    public class DepthViewEffect : PostProcessEffect
    {
        private Shader shader;
        private readonly SceneRenderer renderer;

        private float nearPlane, farPlane;

        public DepthViewEffect(Shader shader, SceneRenderer renderer, float nearPlane = 0.1f, float farPlane = 100f)
        {
            this.shader = shader;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.renderer = renderer;
        }

        public override void Apply(int _unused, int outputFbo, Vector2i clientSize)
        {
            shader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderer.DepthTexture);
            shader.SetInt("uDepthMap", 0);
            var cam = Camera.Main;
            if (cam == null) return;
            shader.SetFloat("uNear", 0.1f);
            shader.SetFloat("uFar", 800f);
        }
    }
}
