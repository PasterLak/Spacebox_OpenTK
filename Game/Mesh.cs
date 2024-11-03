using OpenTK.Graphics.OpenGL4;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Mesh // Digital Differential Analyzer for collision
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _vertexCount;

        PolygonMode polygonMode = PolygonMode.Fill;

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

            int stride = (3 + 2 + 3) * sizeof(float); // position(3) + uv(2) + color(3)

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (3 + 2) * sizeof(float));
            GL.EnableVertexAttribArray(2);

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
            GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);
            GL.Enable(EnableCap.CullFace);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
