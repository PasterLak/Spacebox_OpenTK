// SphereEmitter.cs
using OpenTK.Mathematics;

namespace Engine
{
    public class SphereEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public float Radius = 1f;

        public override Particle Create()
        {
            float z = NextFloat() * 2f - 1f;
            float t = NextFloat() * MathF.Tau;
            float rxy = MathF.Sqrt(1f - z * z);
            var dir = new Vector3(rxy * MathF.Cos(t), rxy * MathF.Sin(t), z);
            var pos = Center + dir * Radius * MathF.Pow(NextFloat(), 1f / 3f);
            var vel = dir * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);
            var p = new Particle(pos, vel, life, ColorStart, ColorEnd, startSize, endSize);
            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);
            return p;
        }

        public override void Debug()
        {
            if (ParticleSystem == null) return;
            VisualDebug.DrawSphere(ParticleSystem.PositionWorld, Radius, 8, Color4.Cyan);
        }
    }
}
