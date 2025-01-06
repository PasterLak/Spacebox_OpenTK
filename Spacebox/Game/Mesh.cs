using OpenTK.Graphics.OpenGL4;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Mesh // DDA Digital Differential Analyzer for collision
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _vertexCount;

        private static PolygonMode polygonMode = PolygonMode.Fill;

        public bool EnableBlend = true;
        public bool EnableDepthTest = true;
        public bool EnableAlpha = true;

        public const int FloatsPerVertex = (3 + 2 + 3 + 3 + 2);
        public Mesh(float[] vertices, uint[] indices)
        {
            _vertexCount = indices.Length;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

          
            int stride = sizeof(float) * FloatsPerVertex; // !!! dont forget to change !!! 
            int offset = 0;

            // Position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(0);
            offset += sizeof(float) * 3;

            // TexCoord
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(1);
            offset += sizeof(float) * 2;

            // Color
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(2);
            offset += sizeof(float) * 3;

            // Normal
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(3);
            offset += sizeof(float) * 3;

            // AO
            GL.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(4);
            

            GL.BindVertexArray(0);
        }

        public void Draw(Shader shader)
        {
            shader.Use();
            if(Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F10))
            {
                if(polygonMode == PolygonMode.Line)
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
          

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.CullFace);
           
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            if(FramebufferCapture.IsActive)
                FramebufferCapture.SaveFrame();
            
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
