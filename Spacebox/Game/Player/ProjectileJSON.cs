using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game.Player
{
    public class ProjectileJSON
    {
        public string Name { get;  set; } = "projectiledefault";
        public float Speed { get; set; } = 5;
        public int MaxTravelDistance { get; set; } = 100;
        public float Length { get; set; } = 1f;
        public float Thickness { get; set; } = 0.2f;
        public Color3Byte Color { get; set; } = new Color3Byte(0,0,255);


        public int Damage { get; set; } = 0;
        public float RicochetAngle { get; set; } = 0;
        public int PossibleRicochets { get; set; } = 0;
    }
}
