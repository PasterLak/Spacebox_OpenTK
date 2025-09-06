using OpenTK.Mathematics;

namespace Engine
{
    public class PointEmitter : EmitterBase
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Direction = Vector3.Zero;

        public override Particle Create()
        {
            Vector3 dir;

            if (Direction.LengthSquared < 1e-6f)
            {
                float z = NextFloat() * 2f - 1f;
                float phi = NextFloat() * MathF.Tau;
                float r = MathF.Sqrt(1f - z * z);
                dir = new Vector3(r * MathF.Cos(phi), r * MathF.Sin(phi), z);
            }
            else
            {
                dir = Vector3.Normalize(Direction);
            }

            var vel = dir * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);

            var p = ParticleSystem.CreateParticle().Init(Position, vel, life, ColorStart, ColorEnd, startSize, endSize);

            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);


            return p;
        }

        public override void Debug()
        {
            if (ParticleSystem == null) return;
            VisualDebug.DrawSphere(ParticleSystem.PositionWorld + Position, 0.05f, 6, Color4.Yellow);
        }
    }
}
