using System.Numerics;

namespace Spacebox.Game
{
    [Serializable]
    public class ChunkData
    {
        public Vector3 Position;
        public short[] BlockIds; // Размер: Size * Size * Size
    }
}
