using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Engine;

namespace Spacebox.PostProcessing
{
    public class SSAOEffect
    {
        private int _gBuffer;
        private int _gPosition, _gNormal, _gAlbedo;
        private int _rboDepth;

        private int _ssaoFBO, _ssaoBlurFBO;
        private int _ssaoColorBuffer, _ssaoColorBufferBlur;
        private int _noiseTexture;
        private List<Vector3> _ssaoKernel;

        private Shader _ssaoShader, _ssaoBlurShader, _lightingShader;

        private int _screenWidth, _screenHeight;
        private Matrix4 _projection;

        public SSAOEffect(int screenWidth, int screenHeight, Matrix4 projection)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _projection = projection;
            Initialize();
        }

        private void Initialize()
        {
            SetupGBuffer();
            GenerateSSAOKernel();
            GenerateNoiseTexture();
            SetupSSAOFBOs();
            LoadShaders();
        }

        private void SetupGBuffer()
        {
            _gBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);

            // Position color buffer
            _gPosition = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _gPosition);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, _screenWidth, _screenHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gPosition, 0);

            // Normal color buffer
            _gNormal = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _gNormal);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, _screenWidth, _screenHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _gNormal, 0);

            // Color + Specular color buffer
            _gAlbedo = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _gAlbedo);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _screenWidth, _screenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, _gAlbedo, 0);

            // Tell OpenGL which color attachments we'll use (of this framebuffer) for rendering 
            DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 };
            GL.DrawBuffers(attachments.Length, attachments);

            // Create and attach depth buffer (renderbuffer)
            _rboDepth = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rboDepth);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, _screenWidth, _screenHeight);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rboDepth);

            // Finally check if framebuffer is complete
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine("GBuffer Framebuffer not complete!");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void GenerateSSAOKernel()
        {
            _ssaoKernel = new List<Vector3>();
            Random rand = new Random();
            for (int i = 0; i < 64; ++i)
            {
                Vector3 sample = new Vector3(
                    (float)(rand.NextDouble() * 2.0 - 1.0),
                    (float)(rand.NextDouble() * 2.0 - 1.0),
                    (float)(rand.NextDouble())
                );
                sample = sample.Normalized();
                sample *= (float)(rand.NextDouble());
                float scale = (float)i / 64.0f;
                sample *= MathHelper.Lerp(0.1f, 1.0f, scale * scale);
                _ssaoKernel.Add(sample);
            }
        }

        private void GenerateNoiseTexture()
        {
            Vector3[] ssaoNoise = new Vector3[16];
            Random rand = new Random();
            for (int i = 0; i < 16; i++)
            {
                ssaoNoise[i] = new Vector3(
                    (float)(rand.NextDouble() * 2.0 - 1.0),
                    (float)(rand.NextDouble() * 2.0 - 1.0),
                    0.0f
                );
            }

            _noiseTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _noiseTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, 4, 4, 0, PixelFormat.Rgb, PixelType.Float, ssaoNoise);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        private void SetupSSAOFBOs()
        {
            // SSAO Framebuffer
            _ssaoFBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssaoFBO);
            _ssaoColorBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _ssaoColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, _screenWidth, _screenHeight, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _ssaoColorBuffer, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine("SSAO Framebuffer not complete!");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // SSAO Blur Framebuffer
            _ssaoBlurFBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssaoBlurFBO);
            _ssaoColorBufferBlur = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _ssaoColorBufferBlur);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, _screenWidth, _screenHeight, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _ssaoColorBufferBlur, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine("SSAO Blur Framebuffer not complete!");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }


        private void LoadShaders()
        {
            _ssaoShader = new Shader("Shaders/SSAO/ssao");
            _ssaoBlurShader = new Shader("Shaders/SSAO/ssao_blur");
            _lightingShader = new Shader("Shaders/SSAO/lighting");
        }

        public void RenderGBuffer(Action renderScene)
        {
            // Первый проход: рендеринг в G-буфер
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            renderScene();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void RenderSSAO()
        {
            // Второй проход: вычисление SSAO
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssaoFBO);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _ssaoShader.Use();
            _ssaoShader.SetVector2("screenSize", Window.Instance.Size);
          
            for (int i = 0; i < _ssaoKernel.Count; ++i)
            {
                _ssaoShader.SetVector3($"samples[{i}]", _ssaoKernel[i]);
            }
            _ssaoShader.SetMatrix4("projection", _projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _gPosition);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _gNormal);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, _noiseTexture);

            RenderQuad();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void RenderSSAOBlur()
        {
            // Третий проход: размытие SSAO
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssaoBlurFBO);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _ssaoBlurShader.Use();
            _ssaoBlurShader.SetFloat("screenWidth", Window.Instance.Size.X);
            _ssaoBlurShader.SetFloat("screenHeight", Window.Instance.Size.Y);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _ssaoColorBuffer);

            RenderQuad();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void RenderLighting()
        {
            // Четвертый проход: финальное освещение
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _lightingShader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _gPosition);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _gNormal);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, _gAlbedo);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, _ssaoColorBufferBlur);

            RenderQuad();
        }

        private int _quadVAO = 0;
        private int _quadVBO;
        private void RenderQuad()
        {
            if (_quadVAO == 0)
            {
                float[] quadVertices = {
                    // positions        // texture Coords
                    -1.0f,  1.0f, 0.0f, 1.0f,
                    -1.0f, -1.0f, 0.0f, 0.0f,
                     1.0f, -1.0f, 1.0f, 0.0f,

                    -1.0f,  1.0f, 0.0f, 1.0f,
                     1.0f, -1.0f, 1.0f, 0.0f,
                     1.0f,  1.0f, 1.0f, 1.0f
                };
                _quadVAO = GL.GenVertexArray();
                _quadVBO = GL.GenBuffer();
                GL.BindVertexArray(_quadVAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            }
            GL.BindVertexArray(_quadVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(_gBuffer);
            GL.DeleteFramebuffer(_ssaoFBO);
            GL.DeleteFramebuffer(_ssaoBlurFBO);
            GL.DeleteTexture(_gPosition);
            GL.DeleteTexture(_gNormal);
            GL.DeleteTexture(_gAlbedo);
            GL.DeleteTexture(_ssaoColorBuffer);
            GL.DeleteTexture(_ssaoColorBufferBlur);
            GL.DeleteTexture(_noiseTexture);
            GL.DeleteRenderbuffer(_rboDepth);

            _ssaoShader.Dispose();
            _ssaoBlurShader.Dispose();
            _lightingShader.Dispose();
        }
    }
}
