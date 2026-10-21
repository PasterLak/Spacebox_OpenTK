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
        public Vector3Byte Size { get; set; } = new Vector3Byte(1);
        public int CellPixels { get; set; } = 0;
        public string Model { get; set; } = "";
        public OxygenGeneratorJSON OxygenGenerator { get; set; } = new OxygenGeneratorJSON();
        public DoorJSON Door { get; set; } = new DoorJSON();
        public SignJSON Sign { get; set; } = new SignJSON();
        public PictureFrameJSON Frame { get; set; } = new PictureFrameJSON();
        public PowerGeneratorJSON Generator { get; set; } = new PowerGeneratorJSON();
        public GravityGeneratorJSON Gravity { get; set; } = new GravityGeneratorJSON();
        public DnaReconstructorJSON DnaReconstructor { get; set; } = new DnaReconstructorJSON();
        public ExplosiveJSON Explosive { get; set; }
        public PipeJSON Pipe { get; set; } = new PipeJSON();
        public DisassemblerJSON Disassembler { get; set; } = new DisassemblerJSON();
        public ThrusterJSON Thruster { get; set; } = new ThrusterJSON();
        public ShipConsoleJSON Console { get; set; } = new ShipConsoleJSON();
        public CannonJSON Cannon { get; set; } = new CannonJSON();
        public ShipCameraJSON ShipCamera { get; set; } = new ShipCameraJSON();
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