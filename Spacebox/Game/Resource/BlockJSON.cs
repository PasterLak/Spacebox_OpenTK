using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource
{
    public class BlockJSON
    {
        public string ID { get; set; } = "no_id";
        public string Name { get; set; } = "NoName";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "block";
        public string Category { get; set; } = "";
        public int PowerToDrill { get; set; } = 1;
        public int Mass { get; set; } = 1;
        public int Durability { get; set; } = 1;
        public float Efficiency { get; set; } = 1f;

        public TextureGroup Textures { get; set; } = new TextureGroup();
        public SoundGroup Sounds { get; set; } = new SoundGroup();

        public string Drop { get; set; } = "$self";
        public int DropQuantity { get; set; } = 1;

        public Vector3SByte FrontDirection { get; set; } = Block.DefaultDirectionVector;

        public bool IsTransparent { get; set; } = false;
        public Color3Byte LightColor { get; set; } = Color3Byte.Black;
        public Vector2Byte StorageSize { get; set; } = new Vector2Byte(3, 3);
    }

    public class TextureGroup
    {
        public FaceTextures Active { get; set; } = new FaceTextures();
        public FaceTextures Inactive { get; set; } = new FaceTextures();
    }

    public class FaceTextures
    {
        public string All { get; set; } = "";
        public string Up { get; set; } = "";
        public string Down { get; set; } = "";
        public string Left { get; set; } = "";
        public string Right { get; set; } = "";
        public string Forward { get; set; } = "";
        public string Back { get; set; } = "";
    }

    public class SoundGroup
    {
        public string Place { get; set; } = "blockPlaceDefault";
        public string Destroy { get; set; } = "blockDestroyDefault";
    }
}