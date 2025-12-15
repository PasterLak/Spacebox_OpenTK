using OpenTK.Mathematics;

namespace Spacebox.Game.Resource
{
    public class StorageBlockData : BlockData
    {
        public Vector2Byte Size { get; private set; } = new Vector2Byte(3, 3);
        public StorageBlockData(string name, string type, Vector2Byte textureCoords, bool isTransparent = false,
            Vector3? lightColor = null) : base(name, type, textureCoords, isTransparent, lightColor)
        {
        }

        public StorageBlockData(BlockData baseBlock, Vector2Byte size) : base(baseBlock)
        {
            Size = size;

            
        }


    }
}
