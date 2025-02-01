using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class BufferShader : IDisposable
    {
        public int VAO { get; private set; }
        public int VBO { get; private set; }
        public int EBO { get; private set; }

        private List<Attribute> attributes = new List<Attribute>();
        public byte FloatsPerVertex { get; private set; } = 0;

        private bool _isGenerated = false;
        public struct Attribute
        {
            public string Name;
            public byte Size;
        }

        public BufferShader(Attribute[] attributes)
        {
            GenBuffer();
            for (byte i = 0; i < attributes.Length; i++)
            {
                AddAttribute(attributes[i].Name, attributes[i].Size);
            }
        }
        public BufferShader()
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
            if (attributes == null) attributes = new List<Attribute>();

            var atr = new Attribute();
            atr.Name = name;
            atr.Size = size;
            attributes.Add(atr);

            FloatsPerVertex = (byte)(FloatsPerVertex + size);
        }
        public void SetAttributes()
        {

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

        }
    }
}
