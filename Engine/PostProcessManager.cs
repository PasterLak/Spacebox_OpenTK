
using Engine;
using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public abstract class PostProcessEffect
    {
        public abstract void Apply(int inputTexture, int outputFbo);
    }

    public class PostProcessManager : IDisposable
    {
        private List<PostProcessEffect> effects = new List<PostProcessEffect>();
        private bool disposed = false;

        public void AddEffect(PostProcessEffect effect)
        {
            effects.Add(effect);
        }

        public void RemoveEffect(PostProcessEffect effect)
        {
            if (effects.Contains(effect))
            {
                effects.Remove(effect);
                if (effect is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public void ClearEffects()
        {
            foreach (var effect in effects)
            {
                if (effect is IDisposable disposable)
                    disposable.Dispose();
            }
            effects.Clear();
        }

        public void Process(int sceneTexture, int finalFbo)
        {
            int currentTexture = sceneTexture;
            foreach (var effect in effects)
            {
                effect.Apply(currentTexture, finalFbo);
                currentTexture = finalFbo;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ClearEffects();
                }
                disposed = true;
            }
        }
    }
}

public class TestColorReplaceEffect : PostProcessEffect
{
    private Shader shader;
    public TestColorReplaceEffect(Shader shader)
    {
        this.shader = shader;
    }
    public override void Apply(int inputTexture, int outputFbo)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, outputFbo);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        shader.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, inputTexture);
        RenderQuad();
    }
    private void RenderQuad()
    {
        // Реализация рендеринга полноэкранного квадрата
    }

}
