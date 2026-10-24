using Engine.Graphics.Rhi;

namespace Engine
{
    public struct BufferAttribute
    {
        public string Name;
        public byte Size;
        public VertexComponentType Type;
        public bool Normalized;

        public BufferAttribute(string name, byte size)
        {
            Name = name;
            Size = size;
        }

        public VertexComponentType ComponentType => Type;

        public int ByteSize => Size * ComponentSize(ComponentType);

        public static int ComponentSize(VertexComponentType type) => type switch
        {
            VertexComponentType.Byte or VertexComponentType.UnsignedByte => 1,
            VertexComponentType.Short or VertexComponentType.UnsignedShort or VertexComponentType.HalfFloat => 2,
            VertexComponentType.Double => 8,
            _ => 4
        };
    }
    public class MeshBuffer : IDisposable, IResource
    {
        public int VAO => _vertexArray.Id;
        public int VBO => _vertexBuffer.Id;
        public int EBO => _indexBuffer.Id;
        public VertexArrayHandle VertexArray => _vertexArray;

        private VertexArrayHandle _vertexArray;
        private BufferHandle _vertexBuffer;
        private BufferHandle _indexBuffer;

        private List<BufferAttribute> attributes = new List<BufferAttribute>();
        public byte FloatsPerVertex { get; private set; } = 0;
        public bool IsDisposed { get ; set ; }

        public MeshBuffer(BufferAttribute[] attributes)
        {
            GenBuffer();
            for (byte i = 0; i < attributes.Length; i++)
            {
                AddAttribute(attributes[i].Name, attributes[i].Size, attributes[i].ComponentType, attributes[i].Normalized);
            }

            ResourceScopes.Track(this);
        }
        public MeshBuffer()
        {
            GenBuffer();
        }

        private void GenBuffer()
        {
            if (Application.IsHeadless) return;

            var device = Gpu.Device;
            _vertexArray = device.CreateVertexArray();
            _vertexBuffer = device.CreateBuffer();
            _indexBuffer = device.CreateBuffer();
        }

        public void BindBuffer(ref float[] vertices, ref uint[] indices)
        {
            Upload(vertices, vertices.Length, indices, indices.Length);
        }

        public void Upload(float[] vertices, int vertexCount, uint[] indices, int indexCount)
        {
            if (VAO == 0) return;

            var device = Gpu.Device;
            device.BindVertexArray(_vertexArray);
            device.BufferData(BufferKind.Vertex, _vertexBuffer, new ReadOnlySpan<float>(vertices, 0, vertexCount), BufferUsage.Static);
            device.BufferData(BufferKind.Index, _indexBuffer, new ReadOnlySpan<uint>(indices, 0, indexCount), BufferUsage.Static);
        }

        public void AddAttribute(string name, byte size)
        {
            AddAttribute(name, size, VertexComponentType.Float, false);
        }

        public void AddAttribute(string name, byte size, VertexComponentType type, bool normalized)
        {
            if (attributes == null) attributes = new List<BufferAttribute>();

            var atr = new BufferAttribute(name, size) { Type = type, Normalized = normalized };
            if (atr.ByteSize % sizeof(float) != 0)
                throw new ArgumentException($"Attribute {name} must use a multiple of 4 bytes, got {atr.ByteSize}");

            attributes.Add(atr);
            FloatsPerVertex = (byte)(FloatsPerVertex + atr.ByteSize / sizeof(float));
        }
        public void SetAttributes()
        {
            if (VAO == 0) return;

            var device = Gpu.Device;
            device.BindVertexArray(_vertexArray);
            device.BindBuffer(BufferKind.Vertex, _vertexBuffer);

            int stride = sizeof(float) * FloatsPerVertex;
            int offset = 0;

            for (byte i = 0; i < attributes.Count; i++)
            {
                var atr = attributes[i];
                AddAttribPointer(device, i, atr, stride, ref offset);
            }

            device.BindVertexArray(VertexArrayHandle.None);
        }

        private static void AddAttribPointer(IGraphicsDevice device, int index, BufferAttribute attribute, int stride, ref int offset)
        {
            device.SetVertexAttribute(index, attribute.Size, attribute.ComponentType, attribute.Normalized, stride, offset);
            offset += attribute.ByteSize;
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            if (VAO != 0)
            {
                var device = Gpu.Device;
                device.DeleteBuffer(_vertexBuffer);
                device.DeleteBuffer(_indexBuffer);
                device.DeleteVertexArray(_vertexArray);
            }

            IsDisposed = true;
            ResourceScopes.Forget(this);

        }

        public IResource Load(string path)
        {
          return null;
        }

        public string GetResourceInfo()
        {
          return $"[MeshBuffer] VAO: {VAO}, VBO: {VBO}, EBO: {EBO}, Attributes: {attributes.Count}, FloatsPerVertex: {FloatsPerVertex}";
        }

        public int GetHandle()
        {
            return VAO;
        }
    }
}
