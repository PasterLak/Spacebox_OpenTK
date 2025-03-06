
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Engine;
using Spacebox.Scenes.Test;

public class CubeRenderer2 : SceneNode, IDisposable, IGameComponent
{
        public bool Enabled = true;
        private Shader _shader;
        private MeshBuffer _buffer;

        private Vector3 _position;
        private Color4 _color = Color4.White;
        public Color4 Color
        {
            get => _color;
            set { _color = value; }
        }

        private readonly float[] _vertices = new float[]
        {
            // Position         // Normal
            // Front face
            -0.5f, -0.5f,  0.5f,  0f,  0f,  1f,
             0.5f, -0.5f,  0.5f,  0f,  0f,  1f,
             0.5f,  0.5f,  0.5f,  0f,  0f,  1f,
            -0.5f,  0.5f,  0.5f,  0f,  0f,  1f,

            // Back face
            -0.5f, -0.5f, -0.5f,  0f,  0f, -1f,
             0.5f, -0.5f, -0.5f,  0f,  0f, -1f,
             0.5f,  0.5f, -0.5f,  0f,  0f, -1f,
            -0.5f,  0.5f, -0.5f,  0f,  0f, -1f,

            // Left face
            -0.5f, -0.5f, -0.5f, -1f,  0f,  0f,
            -0.5f, -0.5f,  0.5f, -1f,  0f,  0f,
            -0.5f,  0.5f,  0.5f, -1f,  0f,  0f,
            -0.5f,  0.5f, -0.5f, -1f,  0f,  0f,

            // Right face
             0.5f, -0.5f, -0.5f,  1f,  0f,  0f,
             0.5f, -0.5f,  0.5f,  1f,  0f,  0f,
             0.5f,  0.5f,  0.5f,  1f,  0f,  0f,
             0.5f,  0.5f, -0.5f,  1f,  0f,  0f,

            // Top face
            -0.5f,  0.5f, -0.5f,  0f,  1f,  0f,
            -0.5f,  0.5f,  0.5f,  0f,  1f,  0f,
             0.5f,  0.5f,  0.5f,  0f,  1f,  0f,
             0.5f,  0.5f, -0.5f,  0f,  1f,  0f,

            // Bottom face
            -0.5f, -0.5f, -0.5f,  0f, -1f,  0f,
            -0.5f, -0.5f,  0.5f,  0f, -1f,  0f,
             0.5f, -0.5f,  0.5f,  0f, -1f,  0f,
             0.5f, -0.5f, -0.5f,  0f, -1f,  0f,
        };

        private readonly uint[] _indices = new uint[]
{
    // Front 
    0, 1, 2,
    2, 3, 0,

    // Back 
    6, 5, 4,
    4, 7, 6,

    // Left 
    8, 9, 10,
    10, 11, 8,

    // Right 
    14, 13, 12,
    12, 15, 14,

    // Top 
    16, 17, 18,
    18, 19, 16,

    // Bottom 
    22, 21, 20,
    20, 23, 22
};


        public CubeRenderer2(Vector3 position)
        {
            _position = position;
            Position = position;
            _shader = ShaderManager.GetShader("Shaders/colored");
            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPos",    Size = 3 },
                new BufferAttribute { Name = "aNormal", Size = 3 }
            };
            _buffer = new MeshBuffer(attrs);
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
        }

        public void Render()
        {
            if (!Enabled) return;
            if (Camera.Main == null) return;
            var cam = Camera.Main;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            _shader.Use();
            var matModel = WorldMatrix;
            _shader.SetMatrix4("model", matModel);
            _shader.SetMatrix4("view", cam.GetViewMatrix());
            _shader.SetMatrix4("projection", cam.GetProjectionMatrix());
            _shader.SetVector4("color", (Vector4)_color);

            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.CullFace);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            _shader?.Dispose();
        }
    }

