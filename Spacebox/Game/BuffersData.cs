

namespace Spacebox.Game
{
    public static class BuffersData
    {

        public const byte FloatsPerVertexBlock = 13;
        public static Buffer CreateBlockBuffer()
        {
            Buffer buffer = new Buffer();
            buffer.AddAttribute("aPosition", 3);
            buffer.AddAttribute("aTexCoord", 2);
            buffer.AddAttribute("aColor", 3);
            buffer.AddAttribute("aNormal", 3);
            buffer.AddAttribute("aAO", 2);

            return buffer;
        }

        public const byte FloatsPerVertexItemModel = 8;
        public static Buffer CreateItemModelBuffer()
        {
            Buffer buffer = new Buffer();
            buffer.AddAttribute("aPosition", 3);
            buffer.AddAttribute("aTexCoord", 2);
            buffer.AddAttribute("aNormal", 3);

            return buffer;
        }
    }
}
