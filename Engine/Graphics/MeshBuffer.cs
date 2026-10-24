using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public struct BufferAttribute
    {
        public string Name;
        public byte Size;
        public VertexAttribPointerType Type;
        public bool Normalized;

        public BufferAttribute(string name, byte size)
        {
            Name = name;
            Size = size;
        }

        public VertexAttribPointerType ComponentType => Type == 0 ? VertexAttribPointerType.Float : Type;

        public int ByteSize => Size * ComponentSize(ComponentType);

        public static int ComponentSize(VertexAttribPointerType type) => type switch
        {
            VertexAttribPointerType.Byte or VertexAttribPointerType.UnsignedByte => 1,
            VertexAttribPointerType.Short or VertexAttribPointerType.UnsignedShort or VertexAttribPointerType.HalfFloat => 2,
            VertexAttribPointerType.Double => 8,
            _ => 4
        };
    }
    public class MeshBuffer : IDisposable, IResource
    {
        public int VAO { get; private set; }
        public int VBO { get; private set; }
        public int EBO { get; private set; }

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

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();
        }

        public void BindBuffer(ref float[] vertices, ref uint[] indices)
        {
            Upload(vertices, vertices.Length, indices, indices.Length);
        }

        public void Upload(float[] vertices, int vertexCount, uint[] indices, int indexCount)
        {
            if (VAO == 0) return;

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexCount * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexCount * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        public void AddAttribute(string name, byte size)
        {
            AddAttribute(name, size, VertexAttribPointerType.Float, false);
        }

        public void AddAttribute(string name, byte size, VertexAttribPointerType type, bool normalized)
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

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int stride = sizeof(float) * FloatsPerVertex;
            int offset = 0;

            for (byte i = 0; i < attributes.Count; i++)
            {
                var atr = attributes[i];
                AddAttribPointer(i, atr, stride, ref offset);
            }

            GL.BindVertexArray(0);
        }

        private static void AddAttribPointer(int index, BufferAttribute attribute, int stride, ref int offset)
        {
            GL.VertexAttribPointer(index, attribute.Size, attribute.ComponentType, attribute.Normalized, stride, offset);
            GL.EnableVertexAttribArray(index);
            offset += attribute.ByteSize;
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            if (VAO != 0)
            {
                GL.DeleteBuffer(VBO);
                GL.DeleteBuffer(EBO);
                GL.DeleteVertexArray(VAO);
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
