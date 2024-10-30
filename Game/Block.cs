using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public enum BlockType
    {
        Air,
        Solid
    }

    public class Block
    {
        public BlockType Type { get; set; }
        public Vector2 TextureCoords { get; set; } // Координаты блока на атласе
        public Vector3 Color { get; set; }

        public bool IsTransparent { get; set; } = false;

        public Block(BlockType type, Vector2 textureCoords, Vector3? color = null)
        {
            Type = type;
            TextureCoords = textureCoords;
            Color = color ?? new Vector3(1.0f, 1.0f, 1.0f);
        }

        public bool IsAir()
        {
            return Type == BlockType.Air;
        }
    }
}
