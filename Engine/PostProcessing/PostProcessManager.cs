using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
namespace Engine.PostProcessing
{
    public abstract class PostProcessEffect
    {
        public abstract void Apply(int inputTexture, int outputFbo, Vector2i clientSize);
    }
    public class PostProcessManager : IDisposable
    {
        private List<PostProcessEffect> effects = new List<PostProcessEffect>();
        private bool disposed = false;
        private static BufferShader fullscreenBuffer;
        private int[] pingpongFBO = new int[2];
        private int[] pingpongTexture = new int[2];
        private int currentPingPong = 0;
        private Vector2i currentBufferSize = new Vector2i(0, 0);
        static PostProcessManager()
        {
            BufferAttribute[] attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPos", Size = 2 },
                new BufferAttribute { Name = "aTexCoords", Size = 2 }
            };
            fullscreenBuffer = new BufferShader(attrs);
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
            for (int i = 0; i < 2; i++)
            {
                if (pingpongFBO[i] != 0)
                {
                    GL.DeleteFramebuffer(pingpongFBO[i]);
                    GL.DeleteTexture(pingpongTexture[i]);
                }
                pingpongFBO[i] = GL.GenFramebuffer();
                pingpongTexture[i] = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, pingpongTexture[i]);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.X, size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, nint.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, pingpongFBO[i]);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, pingpongTexture[i], 0);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
        public void Process(int sceneTexture, Vector2i clientSize)
        {
            if (currentBufferSize != clientSize)
                InitPingPongBuffers(clientSize);
            int readTexture = sceneTexture;
            for (int i = 0; i < effects.Count; i++)
            {
                int writeFBO = pingpongFBO[currentPingPong];
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, writeFBO);
                GL.Viewport(0, 0, clientSize.X, clientSize.Y);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                effects[i].Apply(readTexture, writeFBO, clientSize);
                RenderQuad();
                readTexture = pingpongTexture[currentPingPong];
                currentPingPong = 1 - currentPingPong;
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, clientSize.X, clientSize.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, readTexture);
            RenderQuad();
        }
        public static void RenderQuad()
        {
            GL.BindVertexArray(fullscreenBuffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, nint.Zero);
            GL.BindVertexArray(0);
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
                    for (int i = 0; i < 2; i++)
                    {
                        GL.DeleteFramebuffer(pingpongFBO[i]);
                        GL.DeleteTexture(pingpongTexture[i]);
                    }
                }
                disposed = true;
            }
        }
    }
}
