using System.Numerics;

namespace SpaceNetwork
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Vector3 Color { get; set; }
        public string SkinColor { get; set; } = "White";
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 DisplayedPosition { get; set; }

        public int LastTimeWasActive { get; set; }
    }
}
