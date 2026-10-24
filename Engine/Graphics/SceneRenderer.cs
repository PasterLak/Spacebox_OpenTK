using Engine.Graphics;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine
{
    public class SceneRenderer : IDisposable
    {
        public int Fbo => _fbo.Id;
        public int ColorTexture => _color.Id;
        public int NormalTexture => _normal.Id;
        public int DepthTexture => _depth.Id;
        public FramebufferHandle Framebuffer => _fbo;
        public TextureHandle ColorTextureHandle => _color;
        public TextureHandle NormalTextureHandle => _normal;
        public TextureHandle DepthTextureHandle => _depth;
        public Vector2i Size => new Vector2i(width, height);

        private static readonly Color4 ClearGBuffer = new Color4(0f, 0f, 0f, 0f);
        private static readonly Color4 ClearScene = new Color4(0f, 0f, 0f, 1f);

        private FramebufferHandle _fbo;
        private TextureHandle _color;
        private TextureHandle _normal;
        private TextureHandle _depth;
        private int width, height;
        private bool disposed = false;

        public SceneRenderer(int width, int height)
        {
            this.width = width;
            this.height = height;
            Initialize();
        }

        private void Initialize()
        {
            var device = Gpu.Device;
            _fbo = device.CreateFramebuffer();
            device.BindFramebuffer(_fbo);

            _color = CreateAttachment(device, TextureFormat.Rgba8, SamplerState.LinearRepeat, AttachmentPoint.Color0);
            _normal = CreateAttachment(device, TextureFormat.Rgba8, SamplerState.LinearRepeat, AttachmentPoint.Color1);
            _depth = CreateAttachment(device, TextureFormat.Depth24, SamplerState.NearestRepeat, AttachmentPoint.Depth);

            device.SetDrawBuffers(2);
            device.BindFramebuffer(FramebufferHandle.Default);
        }

        private TextureHandle CreateAttachment(IGraphicsDevice device, TextureFormat format, SamplerState sampler, AttachmentPoint attachment)
        {
            var texture = device.CreateTexture(TextureType.Texture2D);
            device.SetTextureData2D(texture, 0, format, width, height, ReadOnlySpan<byte>.Empty);
            device.SetSampler(texture, TextureType.Texture2D, sampler);
            device.AttachTexture(attachment, texture, 0);
            return texture;
        }

        public void Resize(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;

            DeleteTargets();

            Initialize();
        }

        public void Render(Action renderScene)
        {
            using (GPUDebug.Group("RENDER.SCENE"))
            {
                var device = Gpu.Device;
                device.BindFramebuffer(_fbo);
                device.SetViewport(0, 0, width, height);
                GLState.DepthMask(true);
                device.SetClearColor(ClearScene);
                GLState.GBufferWrite(true);
                device.Clear(ClearTargets.Color | ClearTargets.Depth);
                device.ClearColorAttachment(GLState.GBufferAttachment, ClearGBuffer);
                GLState.GBufferWrite(false);
                GPUDebug.Marker("START");
                renderScene.Invoke();
                GPUDebug.Marker("END");
                device.BindFramebuffer(FramebufferHandle.Default);

            }
        }

        private void DeleteTargets()
        {
            var device = Gpu.Device;
            device.DeleteFramebuffer(_fbo);
            device.DeleteTexture(_color);
            device.DeleteTexture(_normal);
            device.DeleteTexture(_depth);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                DeleteTargets();
                disposed = true;
            }
        }
    }
}
