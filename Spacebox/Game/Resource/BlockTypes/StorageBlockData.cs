using OpenTK.Mathematics;

namespace Spacebox.Game.Resource
{
    public class StorageBlockData : BlockData
    {
        public Vector2Byte Size { get; private set; } = new Vector2Byte(3, 3);
        public StorageBlockData(string name, string type, Vector2Byte textureCoords, bool isTransparent = false,
            Vector3? lightColor = null) : base(name, type, isTransparent, lightColor)
        {
        }

        public static Vector2Byte ClampSize(Vector2Byte size)
        {
            return new Vector2Byte(Math.Max((byte)1, size.X), Math.Max((byte)1, size.Y));
        }

        public StorageBlockData(BlockData baseBlock, Vector2Byte size) : base(baseBlock)
        {
            Size = size;

            
        }


    }
}
