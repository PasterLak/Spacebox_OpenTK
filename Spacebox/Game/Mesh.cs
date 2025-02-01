using Engine;
using OpenTK.Graphics.OpenGL4;


namespace Spacebox.Game
{


    public class Mesh // DDA Digital Differential Analyzer for collision
    {

        private int _vertexCount;

        private static PolygonMode polygonMode = PolygonMode.Fill;

        public bool EnableBlend = true;
        public bool EnableDepthTest = true;
        public bool EnableAlpha = true;

        private BufferShader buffer;
        public readonly int FloatsPerVertex;

        
        public Mesh(float[] vertices, uint[] indices, BufferShader buffer)
        {
            this.buffer = buffer;
            _vertexCount = indices.Length;

            FloatsPerVertex = buffer.FloatsPerVertex;
            buffer.BindBuffer(ref vertices, ref indices);

            buffer.SetAttributes();
        }

        public void Draw(Shader shader)
        {
            shader.Use();
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F10))
            {
                if (polygonMode == PolygonMode.Line)
                {
                    polygonMode = PolygonMode.Fill;
                }

                else
                {
                    polygonMode = PolygonMode.Line;
                }
            }
            //GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);

            GL.Enable(EnableCap.CullFace);

            if (EnableDepthTest)
                GL.Enable(EnableCap.DepthTest);
            if (EnableBlend)
                GL.Enable(EnableCap.Blend);

            if (EnableAlpha)
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            GL.BindVertexArray(buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.CullFace);

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
          //  if (FramebufferCapture.IsActive)
          //      FramebufferCapture.SaveFrame();

        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
