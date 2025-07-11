
using OpenTK.Mathematics;

namespace Engine
{
    public class ConeEmitter : EmitterBase
    {
        public Vector3 Apex = Vector3.Zero;
        public Vector3 Axis = Vector3.UnitY;
        public float Angle = 30f;
        public float Height = 1f;

        public override Particle Create()
        {
            float cosA = MathF.Cos(MathHelper.DegreesToRadians(Angle));
            float u = NextFloat();
            float v = NextFloat();
            float theta = 2f * MathF.PI * u;
            float z = MathHelper.Lerp(1f, cosA, v);
            float rc = MathF.Sqrt(1f - z * z);
            var localDir = new Vector3(rc * MathF.Cos(theta), z, rc * MathF.Sin(theta));

            Quaternion rotation = Quaternion.Identity;
            if (MathF.Abs(Vector3.Dot(Axis.Normalized(), Vector3.UnitY)) < 0.999f)
            {
                var rotAxis = Vector3.Cross(Vector3.UnitY, Axis).Normalized();
                var rotAngle = MathF.Acos(Vector3.Dot(Vector3.UnitY, Axis.Normalized()));
                rotation = Quaternion.FromAxisAngle(rotAxis, rotAngle);
            }

            var dir = Vector3.Transform(localDir, rotation).Normalized();
            var dist = NextFloat() * Height;
            var pos = Apex + dir * dist;
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
            VisualDebug.DrawCone(Apex, Axis, Angle, Height, Color4.Cyan, 12);
        }
    }
}
