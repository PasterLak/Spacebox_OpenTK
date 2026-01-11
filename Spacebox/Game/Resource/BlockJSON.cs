using Engine;

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

        public string Sides { get; set; } = "";
        public string Up { get; set; } = "";
        public string Down { get; set; } = "";

        public string Left { get; set; }
        public string Right { get; set; }
        public string Forward { get; set; }
        public string Back { get; set; }

        // ---

        public string SidesOff { get; set; } = "";
        public string UpOff { get; set; } = "";
        public string DownOff { get; set; } = "";

        public string LeftOff { get; set; }
        public string RightOff { get; set; }
        public string ForwardOff { get; set; }
        public string BackOff { get; set; }

        public string Drop { get; set; } = "$self";
        public int DropQuantity { get; set; } = 1;

        public Vector3SByte FrontDirection { get; set; } = Vector3SByte.Up;

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        public bool IsTransparent { get; set; } = false;
        public Color3Byte LightColor { get; set; } = Color3Byte.Black;
        public Vector2Byte StorageSize { get; set; } = new Vector2Byte(3,3);
    }


}
