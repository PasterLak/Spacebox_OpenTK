using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class CubeRenderer : Node3D, IDisposable
    {
        public bool Enabled = true;
        private Shader _shader;
        private BufferShader _buffer;

        private Vector3 _position;
        private Color4 _color = Color4.White;
        public Color4 Color
        {
            get => _color;
            set { _color = value; }
        }
        private int _textureId;
        public int TextureId
        {
            get => _textureId;
            set { _textureId = value; }
        }

        private readonly float[] _vertices = new float[]
        {
            // pos          // norm         // texture
            // Front face
            -0.5f, -0.5f,  0.5f,  0f,  0f,  1f,  0f, 0f,
             0.5f, -0.5f,  0.5f,  0f,  0f,  1f,  1f, 0f,
             0.5f,  0.5f,  0.5f,  0f,  0f,  1f,  1f, 1f,
            -0.5f,  0.5f,  0.5f,  0f,  0f,  1f,  0f, 1f,

            // Back face
            -0.5f, -0.5f, -0.5f,  0f,  0f, -1f,  1f, 0f,
             0.5f, -0.5f, -0.5f,  0f,  0f, -1f,  0f, 0f,
             0.5f,  0.5f, -0.5f,  0f,  0f, -1f,  0f, 1f,
            -0.5f,  0.5f, -0.5f,  0f,  0f, -1f,  1f, 1f,

            // Left face
            -0.5f, -0.5f, -0.5f, -1f,  0f,  0f,  0f, 0f,
            -0.5f, -0.5f,  0.5f, -1f,  0f,  0f,  1f, 0f,
            -0.5f,  0.5f,  0.5f, -1f,  0f,  0f,  1f, 1f,
            -0.5f,  0.5f, -0.5f, -1f,  0f,  0f,  0f, 1f,

            // Right face
             0.5f, -0.5f, -0.5f,  1f,  0f,  0f,  1f, 0f,
             0.5f, -0.5f,  0.5f,  1f,  0f,  0f,  0f, 0f,
             0.5f,  0.5f,  0.5f,  1f,  0f,  0f,  0f, 1f,
             0.5f,  0.5f, -0.5f,  1f,  0f,  0f,  1f, 1f,

            // Top face
            -0.5f,  0.5f, -0.5f,  0f,  1f,  0f,  0f, 1f,
            -0.5f,  0.5f,  0.5f,  0f,  1f,  0f,  0f, 0f,
             0.5f,  0.5f,  0.5f,  0f,  1f,  0f,  1f, 0f,
             0.5f,  0.5f, -0.5f,  0f,  1f,  0f,  1f, 1f,

            // Bottom face
            -0.5f, -0.5f, -0.5f,  0f, -1f,  0f,  1f, 1f,
            -0.5f, -0.5f,  0.5f,  0f, -1f,  0f,  1f, 0f,
             0.5f, -0.5f,  0.5f,  0f, -1f,  0f,  0f, 0f,
             0.5f, -0.5f, -0.5f,  0f, -1f,  0f,  0f, 1f,
        };

        private readonly uint[] _indices = new uint[]
        {
            // Front 
            0, 1, 2,
            2, 3, 0,

            // Back 
            4, 5, 6,
            6, 7, 4,

            // Left 
            8, 9,10,
           10,11, 8,

            // Right 
           12,13,14,
           14,15,12,

            // Top 
           16,17,18,
           18,19,16,

            // Bottom 
           20,21,22,
           22,23,20
        };

        public CubeRenderer(Vector3 position)
        {
            _position = position;
            Position = position;
            _shader = ShaderManager.GetShader("Shaders/textured");
            var attrs = new BufferShader.Attribute[]
            {
                new BufferShader.Attribute { Name = "aPos",       Size = 3 },
                new BufferShader.Attribute { Name = "aNormal",    Size = 3 },
                new BufferShader.Attribute { Name = "aTexCoords", Size = 2 }
            };
            _buffer = new BufferShader(attrs);
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
            var matModel = GetModelMatrix();
            _shader.SetMatrix4("model", matModel);
            _shader.SetMatrix4("view", cam.GetViewMatrix());
            _shader.SetMatrix4("projection", cam.GetProjectionMatrix());
            _shader.SetVector4("color", (Vector4)_color);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            _shader.SetInt("texture0", 0);
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
}
