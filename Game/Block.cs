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
        public short BlockId { get; set; }
        public BlockType Type { get; set; }
        public Vector2 TextureCoords { get; set; }
        public Vector3 Color { get; set; }

        public bool IsTransparent { get; set; } = false;

        // local data
        public float LightLevel { get; set; } = 0; //0 - 15
        public Vector3 LightColor { get; set; } = Vector3.Zero; // Цвет света

        public Block(BlockType type, Vector2 textureCoords, Vector3? color = null, float lightLevel = 0f, Vector3? lightColor = null)
        {
            Type = type;
            TextureCoords = textureCoords;
            Color = color ?? new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = lightLevel;
            LightColor = lightColor ?? Vector3.Zero;
        }

        public Block(BlockData blockData)
        {
            BlockId = blockData.Id;

            Type = BlockType.Solid;
            IsTransparent = blockData.IsTransparent;
            TextureCoords = blockData.TextureCoords;
            Color =  new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = 0;
            LightColor = blockData.LightColor;

            if(LightColor != Vector3.Zero)
            {
                LightLevel = 15;
            }
        }

        public bool IsAir()
        {
            return Type == BlockType.Air;
        }

        public override string ToString()
        {
            return $"Block type: {Type}, LightLevel: {LightLevel}";
        }
    }


}
