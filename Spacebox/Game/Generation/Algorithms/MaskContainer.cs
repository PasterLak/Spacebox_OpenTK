using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace Spacebox.Generation
{

    public class MaskContainer
    {
        public float sphericalFalloffDistance = 10f;
        public bool sphericalGradient = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte SphericalMaskFunction(byte noiseValue, ref Vector3 pos, ref Vector3 regionSize)
        {
            Vector3 center = regionSize * 0.5f;
            float falloffSqr = sphericalFalloffDistance * sphericalFalloffDistance;
            float distSqr = (pos - center).LengthSquared;

            if (distSqr > falloffSqr)
                return 0;

            float factor = MathHelper.Clamp(1f - distSqr / falloffSqr, 0f, 1f);

            return sphericalGradient ? (byte)(noiseValue * factor) : noiseValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AngularMaskFunction(byte noiseValue, ref Vector3 pos, ref Vector3 regionSize)
        {
            Vector3 half = regionSize * 0.5f;
            Vector3 d = pos - half;
            float nx = MathF.Abs(d.X) / half.X;
            float ny = MathF.Abs(d.Y) / half.Y;
            float nz = MathF.Abs(d.Z) / half.Z;
            float m = MathF.Max(nx, MathF.Max(ny, nz));
            if (m > 1f) return 0;
            if (!sphericalGradient) return noiseValue;
            float factor = MathHelper.Clamp(1f - m, 0f, 1f);
            return (byte)(noiseValue * factor);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte EllipsoidalMaskFunction(byte noiseValue, ref Vector3 pos, ref Vector3 regionSize)
        {
            Vector3 halfSizes = regionSize * 0.5f;

            float nx = (pos.X - halfSizes.X) / halfSizes.X;
            float ny = (pos.Y - halfSizes.Y) / halfSizes.Y;
            float nz = (pos.Z - halfSizes.Z) / halfSizes.Z;

            float sum = nx * nx + ny * ny + nz * nz;

            if (sum > 1f)
                return 0;

            if (sphericalGradient)
            {
                float factor = MathHelper.Clamp(1f - sum, 0f, 1f);
                return (byte)(noiseValue * factor);
            }
            else
            {
                return noiseValue;
            }
        }

    }

}
