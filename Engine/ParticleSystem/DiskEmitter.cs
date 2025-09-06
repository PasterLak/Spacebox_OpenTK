using OpenTK.Mathematics;

namespace Engine
{
    public class DiskEmitter : EmitterBase
    {
        public Vector3 Center = Vector3.Zero;
        public Vector3 Normal = Vector3.UnitY;
        public float Radius = 5f;

        public override Particle Create()
        {
            float rU = MathF.Sqrt(NextFloat()) * Radius;
            float theta = NextFloat() * MathF.Tau;
            var local = new Vector3(rU * MathF.Cos(theta), 0f, rU * MathF.Sin(theta));
            var up = Normal.Normalized();
            var tangent = Vector3.Cross(up, Math.Abs(up.X) < 0.99f ? Vector3.UnitX : Vector3.UnitY).Normalized();
            var bitangent = Vector3.Cross(up, tangent);
            var pos = Center + tangent * local.X + bitangent * local.Z;
            var vel = up * Range(SpeedMin, SpeedMax);
            var life = Range(LifeMin, LifeMax);
            var startSize = Range(StartSizeMin, StartSizeMax);
            var endSize = Range(EndSizeMin, EndSizeMax);
            var p = ParticleSystem.CreateParticle().Init(pos, vel, life, ColorStart, ColorEnd, startSize, endSize);
            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);
            return p;
        }

        public override void Debug()
        {
            VisualDebug.DrawDisk(Center, Normal, Radius, Color4.Cyan, 16);
        }
    }
}