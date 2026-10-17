using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public struct BufferAttribute
    {
        public string Name;
        public byte Size;

        public BufferAttribute(string name, byte size)
        {
            Name = name;
            Size = size;
        }
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
                AddAttribute(attributes[i].Name, attributes[i].Size);
            }

            Resources.AddResourceToControll(this);
        }
        public MeshBuffer()
        {
            GenBuffer();
        }

        private void GenBuffer()
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();
        }

        public void BindBuffer(ref float[] vertices, ref uint[] indices)
        {
            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        public void AddAttribute(string name, byte size)
        {
            if (attributes == null) attributes = new List<BufferAttribute>();

            var atr = new BufferAttribute();
            atr.Name = name;
            atr.Size = size;
            attributes.Add(atr);

            FloatsPerVertex = (byte)(FloatsPerVertex + size);
        }
        public void SetAttributes()
        {
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int stride = sizeof(float) * FloatsPerVertex;
            int offset = 0;

            for (byte i = 0; i < attributes.Count; i++)
            {
                var atr = attributes[i];
                AddAttribPointer(i, atr.Size, ref stride, ref offset, i < attributes.Count - 1);
            }

            GL.BindVertexArray(0);
        }

        private void AddAttribPointer(int index, int size, ref int stride, ref int offset, bool addOffset)
        {
            GL.VertexAttribPointer(index, size, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(index);
            if (addOffset)
                offset += sizeof(float) * size;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteVertexArray(VAO);

            IsDisposed = true;

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
