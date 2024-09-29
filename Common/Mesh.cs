using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace Spacebox.Common
{
    public class Mesh : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _indexCount;

        private BoundingBox _bounds;

        public Mesh(float[] vertices, int[] indices)
        {
            _indexCount = indices.Length;

            ComputeBoundingBox(vertices);

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

            int stride = (3 + 3 + 2) * sizeof(float); // position, normal, texcoord

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

            GL.BindVertexArray(0);
        }

        public BoundingBox GetBounds() { return _bounds?? new BoundingBox(Vector3.Zero, Vector3.Zero) ; }

        private void ComputeBoundingBox(float[] vertices)
        {
            if (vertices.Length < 3)
            {
                _bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            for (int i = 0; i < vertices.Length; i += 8) // Предполагается, что каждая вершина содержит 8 float (позиция, нормаль, текстура)
            {
                Vector3 pos = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]) * 2;  // SIZE fix
                min = Vector3.ComponentMin(min, pos);
                max = Vector3.ComponentMax(max, pos);
            }

            _bounds = new BoundingBox(min, max);
        }


        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }
    }
}
