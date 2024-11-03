using OpenTK.Mathematics;

namespace Spacebox.Game
{
   
    public class Block
    {
        public short BlockId { get; set; } = 0;
       
        public Vector2 TextureCoords { get; set; }
        public Vector3 Color { get; set; }

        public bool IsTransparent { get; set; } = false;

        // local data
        public float LightLevel { get; set; } = 0; //0 - 15
        public Vector3 LightColor { get; set; } = Vector3.Zero; 

        public Block(Vector2 textureCoords, Vector3? color = null, float lightLevel = 0f, Vector3? lightColor = null)
        {
           
            TextureCoords = textureCoords;
            Color = color ?? new Vector3(1.0f, 1.0f, 1.0f);
            LightLevel = lightLevel;
            LightColor = lightColor ?? Vector3.Zero;
        }

        public Block(BlockData blockData)
        {
            BlockId = blockData.Id;

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
            return BlockId == 0;
        }

        public override string ToString()
        {
            return $"Block id: {BlockId}, LightLevel: {LightLevel}";
        }
    }


}
