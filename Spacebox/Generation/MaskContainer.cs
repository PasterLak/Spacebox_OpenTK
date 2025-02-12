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
    }
}
