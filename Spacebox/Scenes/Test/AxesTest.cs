using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Engine;

    public class AxesTest : SceneNode
    {
        private BufferShader _buffer;
        private Shader _shader;
        private float[] vertices;
        private uint[] indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        public float Length { get; set; }

        public AxesTest(Vector3 position, float length)
        {
            Position = position;
            Length = length;
          
            _shader = ShaderManager.GetShader("Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        public AxesTest(Vector3 position, float length, Vector3 rotation)
        {
            Position = position;
            Length = length;
           
            _shader = ShaderManager.GetShader("Shaders/axes");
            UpdateVertices();
            SetupBuffer();
        }

        private void SetupBuffer()
        {
            _buffer = new BufferShader(new BufferAttribute[]
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

        public void Render(Camera camera)
        {
            Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            _shader.Use();
            Matrix4 model = WorldMatrix;

            int modelLocation = GL.GetUniformLocation(_shader.Handle, "model");
            int viewLocation = GL.GetUniformLocation(_shader.Handle, "view");
            int projectionLocation = GL.GetUniformLocation(_shader.Handle, "projection");

            GL.UniformMatrix4(modelLocation, false, ref model);
            GL.UniformMatrix4(viewLocation, false, ref view);
            GL.UniformMatrix4(projectionLocation, false, ref projection);

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
            SetRotation(newRotation);
            UpdateVertices();
        }
    }

