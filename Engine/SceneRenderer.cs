
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine
{


    public class SceneRenderer : IDisposable
    {
        public int Fbo { get; private set; }
        public int SceneTexture { get; private set; }
        public int Rbo { get; private set; }
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

            SceneTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, SceneTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, SceneTexture, 0);

            Rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, Rbo);

            //if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
             //   throw new Exception("Framebuffer is not complete");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Resize(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
            GL.DeleteFramebuffer(Fbo);
            GL.DeleteTexture(SceneTexture);
            GL.DeleteRenderbuffer(Rbo);
            Initialize();
        }

        public void Render(Action renderScene)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
            GL.Viewport(0, 0, width, height);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            renderScene.Invoke();

            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.B))
            {
                FramebufferCapture.SaveScreenshot(new Vector2i(width, height));
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                GL.DeleteFramebuffer(Fbo);
                GL.DeleteTexture(SceneTexture);
                GL.DeleteRenderbuffer(Rbo);
                disposed = true;
            }
        }
    }

}
