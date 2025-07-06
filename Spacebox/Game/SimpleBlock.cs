using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player;

namespace Spacebox
{
    public class SimpleBlock : Node3D, IDisposable, IDrawable
    {
        private float[] _vertices;
        private uint[] _indices;
        private MeshBuffer _buffer;
        public TextureMaterial Material { get; set; }

        public bool IsUsingDefaultUV { get; private set; } = true;
        private Dictionary<Face, Vector2[]> _uvs = new Dictionary<Face, Vector2[]>();
        private bool _isDisposed;
        public SimpleBlock(TextureMaterial material, Vector3 position)
        {
            Position = position;
            Scale = Vector3.One;
            Material = material;
            foreach (var face in Enum.GetValues(typeof(Face)))
            {
                _uvs.Add((Face)face, CubeMeshData.GetBasicUVs());
            }

            _buffer = TextureMaterial.GetMeshBuffer();
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
            foreach (var key in _uvs.Keys)
            {
                _uvs[key] = uv;
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
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
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
                    vertices.Add(faceVertices[i].X - 0.5f);
                    vertices.Add(faceVertices[i].Y - 0.5f);
                    vertices.Add(faceVertices[i].Z - 0.5f);
                    vertices.Add(normal.X);
                    vertices.Add(normal.Y);
                    vertices.Add(normal.Z);
                    vertices.Add(texCoords[i].X);
                    vertices.Add(texCoords[i].Y);
                }
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
                    Debug.Error("False face!");
                    return Vector3.Zero;
            }
        }

        public void Render(Astronaut camera)
        {
            if (_isDisposed)
                return;


            Material.SetUniforms(GetRenderModelMatrix());
            Material.Use();

            GL.BindVertexArray(_buffer.VAO);

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _buffer.Dispose();
            _isDisposed = true;
        }
    }
}
