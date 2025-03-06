/*using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace Spacebox.Game
{
    public class Mesh
    {

        private MeshBuffer buffer;

        private int _vertexCount;

        private static PolygonMode polygonMode = PolygonMode.Fill;
        
        //public readonly int FloatsPerVertex;


        public Mesh(float[] vertices, uint[] indices, MeshBuffer buffer)
        {
            this.buffer = buffer;
            _vertexCount = indices.Length;

          //  FloatsPerVertex = buffer.FloatsPerVertex;
            buffer.BindBuffer(ref vertices, ref indices);

            buffer.SetAttributes();
        }

        public void Render(MaterialBase material, Matrix4 model)
        {
            material.Use();
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

            material.SetUniforms(model);

            GL.BindVertexArray(buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}*/
