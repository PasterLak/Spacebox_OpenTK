using Engine;

namespace Spacebox.Game
{
    public static class BuffersData
    {

        public const byte FloatsPerVertexBlock = Spacebox.Game.Generation.BlockVertex.Words;
        public static MeshBuffer CreateBlockBuffer()
        {
            MeshBuffer buffer = new MeshBuffer();
            buffer.AddAttribute("aPosition", 3);
            buffer.AddAttribute("aTexCoord", 2);
            buffer.AddAttribute("aColor", 4, OpenTK.Graphics.OpenGL4.VertexAttribPointerType.UnsignedByte, true);
            buffer.AddAttribute("aNormal", 4, OpenTK.Graphics.OpenGL4.VertexAttribPointerType.UnsignedByte, true);

            return buffer;
        }

        public const byte FloatsPerVertexItemModel = 8;
        public static MeshBuffer CreateItemModelBuffer()
        {
            MeshBuffer buffer = new MeshBuffer();
            buffer.AddAttribute("aPosition", 3);
            buffer.AddAttribute("aNormal", 3);
            buffer.AddAttribute("aTexCoord", 2);

            return buffer;
        }
    }
}
