using OpenTK.Mathematics;

namespace Engine
{
    public class ConeEmitter : EmitterBase
    {
        public Vector3 Apex = Vector3.Zero;
        public Vector3 Axis = Vector3.UnitY;
        public Vector3 Direction = Vector3.Zero;   // overrides Axis when non-zero
        public float Angle = 30f;
        public float Height = 1f;

        static Quaternion RotationBetween(in Vector3 from, in Vector3 to)
        {
            Vector3 f = from.Normalized();
            Vector3 t = to.Normalized();
            float d = Vector3.Dot(f, t);

            if (d > 0.9999f)            // almost the same
                return Quaternion.Identity;

            if (d < -0.9999f)           // opposite – rotate 180° around any perpendicular axis
            {
                Vector3 axis = Vector3.Cross(f, Vector3.UnitX);
                if (axis.LengthSquared < 1e-6f)
                    axis = Vector3.Cross(f, Vector3.UnitZ);
                axis.Normalize();
                return Quaternion.FromAxisAngle(axis, MathF.PI);
            }

            Vector3 cross = Vector3.Cross(f, t);
            float s = MathF.Sqrt((1 + d) * 2);
            return new Quaternion(cross / s, s * 0.5f).Normalized();
        }

        public override Particle Create()
        {
            Vector3 axisN = Direction.LengthSquared > 0f ? Direction.Normalized() : Axis.Normalized();

            // build world-space direction for this particle
            float cosA = MathF.Cos(MathHelper.DegreesToRadians(Angle));
            float u = NextFloat();
            float v = NextFloat();

            float theta = 2f * MathF.PI * u;
            float z = MathHelper.Lerp(1f, cosA, v);
            float rc = MathF.Sqrt(1f - z * z);
            Vector3 localDir = new(rc * MathF.Cos(theta), z, rc * MathF.Sin(theta));

            Quaternion q = RotationBetween(Vector3.UnitY, axisN);
            Vector3 dir = Vector3.Transform(localDir, q).Normalized();

            Vector3 pos = Apex + dir * (NextFloat() * Height);
            Vector3 vel = dir * Range(SpeedMin, SpeedMax);
            float life = Range(LifeMin, LifeMax);
            float sz0 = Range(StartSizeMin, StartSizeMax);
            float sz1 = Range(EndSizeMin, EndSizeMax);

            var p = ParticleSystem.CreateParticle();

            p.Init(pos, vel, life, ColorStart, ColorEnd, sz0, sz1);
            p.AccStart = AccelerationStart;
            p.AccEnd = AccelerationEnd;
            p.RotationSpeed = Range(RotationSpeedMin, RotationSpeedMax);

            return p;
        }

        public override void Debug()
        {
            Vector3 axisN = Direction.LengthSquared > 0f ? Direction.Normalized() : Axis.Normalized();
            VisualDebug.DrawCone(Apex, axisN, Angle, Height, Color4.Cyan, 12);
        }

    }
}
