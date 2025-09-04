using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public struct ProjectileParameters
    {
        public short ID { get; private set; } = -1;
        public string Name { get; private set; } = "projectiledefault";
        public float Speed { get; private set; }
        public byte Mass { get; private set; } = 1;
        public int MaxTravelDistance { get; private set; }
        public float Length { get; private set; }
        public float Thickness { get; private set; }
        public Color4 Color { get; private set; }

        public Vector3 Color3 => new Vector3(Color.R, Color.G, Color.B);
        public byte Damage { get; private set; }
        public byte DamageBlocks { get; private set; }
        public float RicochetAngle { get; private set; }
        public int PossibleRicochets { get; private set; }


        public ProjectileParameters()
        {
            SetErrorParams(this);
        }

        public ProjectileParameters(short id, ProjectileJSON projectileJSON)
        {
            var p = projectileJSON;

            ID = id;
            Name = p.ID;
            Speed = p.Speed;
            MaxTravelDistance = p.MaxTravelDistance;
            Length = p.Length;
            Thickness = p.Thickness;
            Color = p.Color.ToColor4();

            Mass = (byte)p.Mass;
            Damage = (byte)p.Damage;
            DamageBlocks = (byte)p.DamageBlocks;
            RicochetAngle = p.RicochetAngle;
            PossibleRicochets = p.PossibleRicochets;

        }

        public static ProjectileParameters GetTestProjectile()
        {
            var p = new ProjectileParameters();

            p.ID = short.MaxValue - 1;
            p.Name = "projectiletest";
            p.Speed = 15;
            p.MaxTravelDistance = 100;
            p.Length = 0.75f;
            p.Thickness = 0.1f;
            p.Color = Color4.Blue;

            p.Mass = 2;
            p.Damage = 5;
            p.DamageBlocks = 5;
            p.RicochetAngle = 30;
            p.PossibleRicochets = 5;

            return p;
        }
        public static ProjectileParameters GetErrorProjectile()
        {
            var p = new ProjectileParameters();

            SetErrorParams(p);

            return p;
        }

        private static void SetErrorParams(ProjectileParameters p)
        {
            p.ID = short.MaxValue;
            p.Name = "projectileerror";
            p.Speed = 5;
            p.MaxTravelDistance = 100;
            p.Length = 1f;
            p.Thickness = 0.1f;
            p.Mass = 1;
            p.Color = Color4.Pink;

            p.Damage = 0;
            p.DamageBlocks = 0;
            p.RicochetAngle = 0;
            p.PossibleRicochets = 0;
        }

    }
}
