using Engine.Graphics.Rhi;
using OpenTK.Mathematics;
namespace Engine.PostProcessing
{
    public abstract class PostProcessEffect
    {
        public bool Enabled { get; set; } = true;
        public virtual bool IsRequired => false;

        public bool ShouldApply(bool postProcessingEnabled)
        {
            return Enabled && (postProcessingEnabled || IsRequired);
        }
        public abstract void Apply(int inputTexture, int outputFbo, Vector2i clientSize);
    }
    public class PostProcessManager : IDisposable
    {
        private List<PostProcessEffect> effects = new List<PostProcessEffect>();
        public bool Enabled { get; set; } = true;
        public Shader PresentShader { get; set; }
        private bool disposed = false;
        private static MeshBuffer fullscreenBuffer;
        private FramebufferHandle[] pingpongFBO = new FramebufferHandle[2];
        private TextureHandle[] pingpongTexture = new TextureHandle[2];
        private int currentPingPong = 0;
        private Vector2i currentBufferSize = new Vector2i(0, 0);
        public PostProcessManager()
        {
            EnsureFullscreenQuad();
        }
        private static void EnsureFullscreenQuad()
        {
            if (fullscreenBuffer != null) return;
            BufferAttribute[] attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPos", Size = 2 },
                new BufferAttribute { Name = "aTexCoords", Size = 2 }
            };
            fullscreenBuffer = new MeshBuffer(attrs);
            ResourceScopes.Own(fullscreenBuffer);
            float[] vertices = {
                -1f, -1f, 0f, 0f,
                 1f, -1f, 1f, 0f,
                 1f,  1f, 1f, 1f,
                -1f,  1f, 0f, 1f
            };
            uint[] indices = { 0, 1, 2, 0, 2, 3 };
            fullscreenBuffer.BindBuffer(ref vertices, ref indices);
            fullscreenBuffer.SetAttributes();
        }
        private void InitPingPongBuffers(Vector2i size)
        {
            var device = Gpu.Device;
            for (int i = 0; i < 2; i++)
            {
                if (!pingpongFBO[i].IsDefault)
                {
                    device.DeleteFramebuffer(pingpongFBO[i]);
                    device.DeleteTexture(pingpongTexture[i]);
                }
                pingpongFBO[i] = device.CreateFramebuffer();
                pingpongTexture[i] = device.CreateTexture(TextureType.Texture2D);
                device.SetTextureData2D(pingpongTexture[i], 0, TextureFormat.Rgba8, size.X, size.Y, ReadOnlySpan<byte>.Empty);
                device.SetSampler(pingpongTexture[i], TextureType.Texture2D, SamplerState.LinearRepeat);
                device.BindFramebuffer(pingpongFBO[i]);
                device.AttachTexture(AttachmentPoint.Color0, pingpongTexture[i], 0);
            }
            device.BindFramebuffer(FramebufferHandle.Default);
            currentPingPong = 0;
            currentBufferSize = size;
        }
        public void UpdateResolution(Vector2i newSize)
        {
            if (newSize != currentBufferSize)
                InitPingPongBuffers(newSize);
        }
        public void AddEffect(PostProcessEffect effect)
        {
            effects.Add(effect);
        }
        public void InsertEffect(int index, PostProcessEffect effect)
        {
            effects.Insert(Math.Clamp(index, 0, effects.Count), effect);
        }
        public int IndexOf(PostProcessEffect effect)
        {
            return effects.IndexOf(effect);
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
        public void Process(SceneRenderer sceneRenderer, Vector2i clientSize)
        {
            if (currentBufferSize != clientSize)
                InitPingPongBuffers(clientSize);
            var device = Gpu.Device;
            int readTexture = sceneRenderer.ColorTexture;
            for (int i = 0; i < effects.Count; i++)
            {
                if (!effects[i].ShouldApply(Enabled)) continue;

                var writeFBO = pingpongFBO[currentPingPong];
                device.BindFramebuffer(writeFBO);
                device.SetViewport(0, 0, clientSize.X, clientSize.Y);
                device.Clear(ClearTargets.Color | ClearTargets.Depth);
                effects[i].Apply(readTexture, writeFBO.Id, clientSize);
                RenderQuad();
                readTexture = pingpongTexture[currentPingPong].Id;
                currentPingPong = 1 - currentPingPong;
            }
            device.BindFramebuffer(FramebufferHandle.Default);
           
                device.SetViewport(0, 0, clientSize.X, clientSize.Y);
            device.Clear(ClearTargets.Color | ClearTargets.Depth);
            if (PresentShader != null)
            {
                PresentShader.Use();
                PresentShader.SetInt("scene", 0);
            }
            device.BindTexture(0, readTexture);
            RenderQuad();
        }
        public static void RenderQuad()
        {
            fullscreenBuffer.DrawIndexed(PrimitiveTopology.Triangles, 6);
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
                    var device = Gpu.Device;
                    for (int i = 0; i < 2; i++)
                    {
                        device.DeleteFramebuffer(pingpongFBO[i]);
                        device.DeleteTexture(pingpongTexture[i]);
                    }
                }
                disposed = true;
            }
        }
    }
}
