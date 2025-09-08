using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;

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
        private Matrix4 _blockTransform = Matrix4.Identity;
        private bool _isDisposed;

        public SimpleBlock(TextureMaterial material, Vector3 position) : base(position)
        {
            Scale = Vector3.One;
            Material = material;

            foreach (var face in Enum.GetValues(typeof(Face)))
            {
                _uvs.Add((Face)face, CubeMeshData.GetBasicUVs());
            }

            _buffer = TextureMaterial.GetMeshBuffer();
            RegenerateMesh();
        }

        public void SetBlockTransform(Matrix4 transform)
        {
            _blockTransform = transform;
        }

        public void ResetBlockTransform()
        {
            _blockTransform = Matrix4.Identity;
        }

        public void ResetUV()
        {
            _vertices = null;
            _indices = null;
            IsUsingDefaultUV = true;

            foreach (var face in _uvs.Keys.ToArray())
            {
                _uvs[face] = CubeMeshData.GetBasicUVs();
            }

            RegenerateMesh();
        }

        public void ChangeUV(Vector2[] uv)
        {
            _vertices = null;
            _indices = null;
            IsUsingDefaultUV = false;

            foreach (var face in _uvs.Keys.ToArray())
            {
                _uvs[face] = uv;
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
                    Vector3 vertex = new Vector3(
                        faceVertices[i].X - 0.5f,
                        faceVertices[i].Y - 0.5f,
                        faceVertices[i].Z - 0.5f
                    );

                    vertices.Add(vertex.X);
                    vertices.Add(vertex.Y);
                    vertices.Add(vertex.Z);
                    vertices.Add(normal.X);
                    vertices.Add(normal.Y);
                    vertices.Add(normal.Z);
                    vertices.Add(texCoords[i].X);
                    vertices.Add(texCoords[i].Y);
                }

                indices.AddRange(new uint[] {
                    indexOffset, indexOffset + 1, indexOffset + 2,
                    indexOffset + 2, indexOffset + 3, indexOffset
                });

                indexOffset += 4;
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        private Vector3 GetNormal(Face face)
        {
            return face switch
            {
                Face.Forward => Vector3.UnitZ,
                Face.Back => -Vector3.UnitZ,
                Face.Left => -Vector3.UnitX,
                Face.Right => Vector3.UnitX,
                Face.Up => Vector3.UnitY,
                Face.Down => -Vector3.UnitY,
                _ => Vector3.UnitY
            };
        }

        public void Render()
        {
            if (_isDisposed) return;

            Matrix4 modelMatrix = _blockTransform * GetRenderModelMatrix();
            Material.Apply(modelMatrix);

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