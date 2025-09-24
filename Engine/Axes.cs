using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class Axes : Node3D, IDisposable
    {
        private MeshBuffer _buffer;
        private Shader _shader;
        private float[] vertices;
        private uint[] indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        public float Length { get; set; }

        public Axes(Vector3 position, float length)
        {
            Position = position;
            Length = length;
            Rotation = Vector3.Zero;
            _shader = Resources.Get<Shader>("Resources/Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        public Axes(Vector3 position, float length, Vector3 rotation)
        {
            Position = position;
            Length = length;
            Rotation = rotation;
            _shader = Resources.Get<Shader>("Resources/Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        private void SetupBuffer()
        {
            _buffer = new MeshBuffer(new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 3 }
            });
            _buffer.BindBuffer(ref vertices, ref indices);
            _buffer.SetAttributes();
        }

        private void UpdateVertices()
        {
            vertices = new float[]
            {
                // X axis
                0.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,
                Length, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,

                // Y axis
                0.0f, 0.0f, 0.0f,  0.0f, 1.0f, 0.0f,
                0.0f, Length, 0.0f,  0.0f, 1.0f, 0.0f,

                // Z axis
                0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, Length,  0.0f, 0.0f, 1.0f,
            };

            if (_buffer != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _buffer.VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        public override void Render()
        {
            base.Render();

            Camera cam = Camera.Main;

            if (cam == null) return;

            _shader.Use();
            Matrix4 model = GetRenderModelMatrix();

            _shader.SetMatrix4("model", model, false);
         
            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Lines, indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }



        public void SetPosition(Vector3 newPosition)
        {
            Position = newPosition;
            UpdateVertices();
        }

        public void SetLength(float newLength)
        {
            Length = newLength;
            UpdateVertices();
        }

        public void SetRotation(Vector3 newRotation)
        {
            Rotation = newRotation;
            UpdateVertices();
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
