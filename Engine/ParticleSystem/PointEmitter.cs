using OpenTK.Mathematics;

namespace Engine
{
    public class PointEmitter : EmitterBase
    {
        public Vector3 Position = Vector3.Zero;

        public override Particle Create()
        {
  
            float z = NextFloat() * 2f - 1f;
            float t = NextFloat() * MathF.Tau;
            float rxy = MathF.Sqrt(1f - z * z);
            var dir = new Vector3(rxy * MathF.Cos(t), rxy * MathF.Sin(t), z);

            var pos = Position;
            var vel = dir * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);

            var p = new Particle(pos, vel, life, ColorStart, ColorEnd, startSize, endSize)
            {
                AccStart = AccelerationStart,
                AccEnd = AccelerationEnd,
                RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax)
            };

            return p;
        }

        public override void Debug()
        {
            if (ParticleSystem == null) return;
            VisualDebug.DrawSphere(ParticleSystem.PositionWorld + Position, 0.05f, 6, Color4.Yellow);
        }
    }
}
