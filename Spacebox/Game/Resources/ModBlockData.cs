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
    
        public string Sides { get; set; } = "";
        public string Top { get; set; } = "";
        public string Bottom { get; set; } = "";

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
