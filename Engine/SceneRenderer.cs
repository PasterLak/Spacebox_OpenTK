using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class SceneRenderer : IDisposable
    {
        public int Fbo { get; private set; }
        public int ColorTexture { get; private set; }
        public int NormalTexture { get; private set; }
        public int DepthTexture { get; private set; }

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
            Fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);

            ColorTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);

            NormalTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, NormalTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, NormalTexture, 0);

            DepthTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTexture, 0);

            DrawBuffersEnum[] drawBuffers = new[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1
            };
            GL.DrawBuffers(drawBuffers.Length, drawBuffers);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Resize(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;

            GL.DeleteFramebuffer(Fbo);
            GL.DeleteTexture(ColorTexture);
            GL.DeleteTexture(NormalTexture);
            GL.DeleteTexture(DepthTexture);

            Initialize();
        }

        public void Render(Action renderScene)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
            GL.Viewport(0, 0, width, height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            renderScene.Invoke();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                GL.DeleteFramebuffer(Fbo);
                GL.DeleteTexture(ColorTexture);
                GL.DeleteTexture(NormalTexture);
                GL.DeleteTexture(DepthTexture);
                disposed = true;
            }
        }
    }
}
