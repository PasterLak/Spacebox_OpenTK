using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class FullscreenRenderer
    {
        private int quadVAO;
        private int quadVBO;
        private int quadEBO;

        public FullscreenRenderer()
        {
            InitializeQuad();
        }

        private void InitializeQuad()
        {

            float[] vertices = {
                // pos    // texCoords
                -1f, -1f,    0f, 0f,
                 1f, -1f,    1f, 0f,
                 1f,  1f,    1f, 1f,
                -1f,  1f,    0f, 1f,
            };
            int[] indices = { 0, 1, 2, 0, 2, 3 };

            quadVAO = GL.GenVertexArray();
            quadVBO = GL.GenBuffer();
            quadEBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

          
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
         
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindVertexArray(0);
        }

        public void RenderToScreen(int textureId, Shader shader, Vector2i clientSize)
        {
     
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, clientSize.X, clientSize.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            shader.Use();
 
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            shader.SetInt("scene", 0);

        
            GL.BindVertexArray(quadVAO);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
