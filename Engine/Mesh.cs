using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Physics;
using Engine.Utils;


namespace Engine
{
    public class Mesh : IResource
    {
        private MeshBuffer buffer;
        private int _indexCount;
        private BoundingBox _bounds;
        public List<Vector3> VerticesPositions { get; private set; }

        public Mesh()
        {

        }
        public Mesh(float[] vertices, int[] indices)
        {
            _indexCount = indices.Length;
            VerticesPositions = new List<Vector3>();
            ComputeBoundingBox(vertices);

            BufferAttribute[] attributes = new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "normal", Size = 3 },
                new BufferAttribute { Name = "uv", Size = 2 }
            };

            buffer = new MeshBuffer(attributes);
            uint[] indicesUInt = indices.Select(x => (uint)x).ToArray();
            buffer.BindBuffer(ref vertices, ref indicesUInt);
            buffer.SetAttributes();
        }

        public Mesh(string path)
        {
            var (vertices, indices) = ObjLoader.Load(path);
            _indexCount = indices.Length;
            VerticesPositions = new List<Vector3>();
            ComputeBoundingBox(vertices);
            BufferAttribute[] attributes = new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "normal", Size = 3 },
                new BufferAttribute { Name = "uv", Size = 2 }
            };
            buffer = new MeshBuffer(attributes);
            uint[] indicesUInt = indices.Select(x => (uint)x).ToArray();
            buffer.BindBuffer(ref vertices, ref indicesUInt);
            buffer.SetAttributes();
        }

        public BoundingBox GetBounds() => _bounds ?? new BoundingBox(Vector3.Zero, Vector3.Zero);

        private void ComputeBoundingBox(float[] vertices)
        {
            if (vertices.Length < 3)
            {
                _bounds = new BoundingBox(Vector3.Zero, Vector3.Zero);
                return;
            }
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < vertices.Length; i += 8)
            {
                Vector3 pos = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]) * 2;
                VerticesPositions.Add(pos);
                min = Vector3.ComponentMin(min, pos);
                max = Vector3.ComponentMax(max, pos);
            }
            _bounds = new BoundingBox(min, max);
        }

        public void Draw()
        {
            GL.BindVertexArray(buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public List<Vector3> GetRandomPoints(int count)
        {
            if (VerticesPositions == null || VerticesPositions.Count == 0 || count <= 0 || count > VerticesPositions.Count)
                return new List<Vector3>();
            HashSet<int> indices = new HashSet<int>();
            Random random = new Random();
            while (indices.Count < count)
                indices.Add(random.Next(VerticesPositions.Count));
            List<Vector3> randomPoints = new List<Vector3>(count);
            foreach (int index in indices)
                randomPoints.Add(VerticesPositions[index]);
            return randomPoints;
        }

        public IResource Load(string path)
        {
            return new Mesh(path);
        }


        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
