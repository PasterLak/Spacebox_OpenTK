using Spacebox.Engine;

namespace Spacebox.Game.Resources
{
    public class ModBlockData
    {
        public string Name { get; set; } = "NoName";
        public string Type { get; set; } = "block";
        public string Category { get; set; } = "";
        public int PowerToDrill { get; set; } = 1;
        public int Mass { get; set; } = 1;
        public int Durability { get; set; } = 1;
        public float Efficiency { get; set; } = 1f;

        public string Sides { get; set; } = "";
        public string Top { get; set; } = "";
        public string Bottom { get; set; } = "";

        public string SoundPlace { get; set; } = "blockPlaceDefault";
        public string SoundDestroy { get; set; } = "blockDestroyDefault";

        public bool IsTransparent { get; set; } = false;
        public Color3Byte LightColor { get; set; } = Color3Byte.Black;
    }

    public class InteractiveBlockData : ModBlockData
    {


    }

    public class LightBlockData : ModBlockData
    {


    }
}
