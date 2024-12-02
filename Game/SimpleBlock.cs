using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game;

namespace Spacebox.Common
{
    public class SimpleBlock : IDisposable
    {
        private float[] _vertices;
        private uint[] _indices;
        private int _vao;
        private int _vbo;
        private int _ebo;
        private Shader _shader;
        public Texture2D Texture;
        public Node3D Transform { get; private set; }

        public bool IsUsingDefaultUV { get; private set; } = true;

        private Dictionary<Face, Vector2[]> _uvs = new Dictionary<Face, Vector2[]>();

        public SimpleBlock(Shader shader, Texture2D texture, Vector3 position)
        {
            Transform = new Node3D
            {
                Position = position,
                Scale = Vector3.One
            };
            _shader = shader;
            Texture = texture;
            Texture.Use(TextureUnit.Texture0);

            foreach (var face in Enum.GetValues(typeof(Face)))
            {
                _uvs.Add((Face)face, CubeMeshData.GetBasicUVs());
            }

            RegenerateMesh();
        }

        public void ResetUV()
        {
            _vertices = null;
            _indices = null;
            IsUsingDefaultUV = true;

            foreach (var uv in _uvs.Keys)
            {
                _uvs[uv] = CubeMeshData.GetBasicUVs();
            }

            RegenerateMesh();
        }

        public void ChangeUV(Vector2[] uv)
        {
            _vertices = null;
            _indices = null;
            IsUsingDefaultUV = false;

            foreach (var uvk in _uvs.Keys)
            {
                _uvs[uvk] = uv;
            }

            RegenerateMesh();
        }

        public void ChangeUV(Vector2[] uv, Face face, bool regenerateMesh)
        {
            _vertices = null;
            _indices = null;
            IsUsingDefaultUV = false;

           
            _uvs[face] = uv;

            if (regenerateMesh)
                RegenerateMesh();
        }

        public void RegenerateMesh()
        {
            (_vertices, _indices) = GenerateMeshData();
            InitializeBuffers();
        }

    

        private (float[] vertices, uint[] indices) GenerateMeshData()
        {
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint indexOffset = 0;

            foreach (Face face in Enum.GetValues(typeof(Face)))
            {
                Vector3[] faceVertices = CubeMeshData.GetFaceVertices(face);
                Vector3 normal = GetNormal(face);
                Vector2[] texCoords = _uvs[face];

                for (int i = 0; i < 4; i++)
                {
                    // Смещаем вершины на -0.5f по всем осям, чтобы центр куба был в начале координат
                    vertices.Add(faceVertices[i].X - 0.5f);
                    vertices.Add(faceVertices[i].Y - 0.5f);
                    vertices.Add(faceVertices[i].Z - 0.5f);
                    vertices.Add(normal.X);
                    vertices.Add(normal.Y);
                    vertices.Add(normal.Z);
                    vertices.Add(texCoords[i].X);
                    vertices.Add(texCoords[i].Y);
                }

                // Добавляем индексы с правильным порядком обхода (против часовой стрелки)
                indices.Add(indexOffset);
                indices.Add(indexOffset + 1);
                indices.Add(indexOffset + 2);
                indices.Add(indexOffset + 2);
                indices.Add(indexOffset + 3);
                indices.Add(indexOffset);
                indexOffset += 4;
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        private Vector3 GetNormal(Face face)
        {
            switch (face)
            {
                case Face.Front:
                    return new Vector3(0f, 0f, 1f);
                case Face.Back:
                    return new Vector3(0f, 0f, -1f);
                case Face.Left:
                    return new Vector3(-1f, 0f, 0f);
                case Face.Right:
                    return new Vector3(1f, 0f, 0f);
                case Face.Top:
                    return new Vector3(0f, 1f, 0f);
                case Face.Bottom:
                    return new Vector3(0f, -1f, 0f);
                default:
                    return Vector3.Zero;
            }
        }

        private void InitializeBuffers()
        {
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);

            // Позиция
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            // Нормаль
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

            // Текстурные координаты
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

            GL.BindVertexArray(0);
        }

        public void Render(Camera camera)
        {
            
            _shader.Use();

    
            Matrix4 model = camera.CameraRelativeRender ?  
                Transform.GetModelMatrixRelativeToCamera(camera) : Transform.GetModelMatrix();

            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", camera.GetViewMatrix());
            _shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            _shader.SetInt("texture0", 0);
            Texture.Use(TextureUnit.Texture0);

            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.DepthTest);

            if (FramebufferCapture.IsActive)
                FramebufferCapture.SaveFrame();
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
            _shader.Dispose();
            Texture.Dispose();
        }
    }
}
