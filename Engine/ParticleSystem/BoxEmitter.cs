using OpenTK.Mathematics;

namespace Engine
{
    public class BoxEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public Vector3 Size = Vector3.One;
        public Vector3 Direction = Vector3.UnitY;

        public override Particle Create()
        {
            var pos = new Vector3(
                (NextFloat() - 0.5f) * Size.X,
                (NextFloat() - 0.5f) * Size.Y,
                (NextFloat() - 0.5f) * Size.Z
            ) + Center;
            var vel = Direction.Normalized() * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);
            //var p = new Particle(pos, vel, life, ColorStart, ColorEnd, startSize, endSize);

            var p = ParticleSystem.CreateParticle();
            p.Position = pos;
            p.Velocity = vel;
            p.Life = life;
            p.StartSize = startSize;
            p.EndSize = endSize;
            p.ColorStart = ColorStart;
            p.ColorEnd = ColorEnd;
            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);
            return p;
        }

        public override void Debug()
        {
            VisualDebug.DrawBox(Center, Size, Color4.Cyan);
        }
    }
}