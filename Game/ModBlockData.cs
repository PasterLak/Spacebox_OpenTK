

namespace Spacebox.Game
{
    public class ModBlockData
    {
        public string Name { get; set; } = "NoName";
        public string Type { get; set; } = "block";
        public int PowerToDrill { get; set; } = 1;
        public int Mass { get; set; } = 1;
        public int Durability { get; set; } = 1;

        public string WallsTexture { get; set; } = "";
        public string TopTexture { get; set; } = "";
        public string BottomTexture { get; set; } = "";
        public Vector2Byte Walls { get; set; } = Vector2Byte.Zero;
        public Vector2Byte Top { get; set; } = Vector2Byte.Zero;
        public Vector2Byte Bottom { get; set; } = Vector2Byte.Zero;
        public bool IsTransparent { get; set; } = false;
        public Vector3Byte LightColor { get; set; } = Vector3Byte.Zero;
    }

    public class InteractiveBlockData : ModBlockData
    {


    }

    public class LightBlockData : ModBlockData
    {


    }
}
