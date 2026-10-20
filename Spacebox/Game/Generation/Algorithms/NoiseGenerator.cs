using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace Spacebox.Generation
{
    public class NoiseGenerator
    {
        private int baseSeed;
        public MaskContainer Masks;

        public NoiseGenerator(int seed)
        {
            this.baseSeed = seed;
            this.Masks = new MaskContainer();
        }

        public byte PerlinNoise3D(float x, float y, float z, byte octaves)
        {
            octaves = (byte)Math.Max(octaves, (byte)1);
            float scale = (octaves == 1) ? 1f : 1f / (float)(2 << (octaves - 1));
            float amplitude = 0.5f;
            float noiseAccum = 0f;
            for (int i = 0; i < octaves; i++)
            {
                noiseAccum += PerlinNoise3D(x * scale, y * scale, z * scale) * amplitude;
                amplitude *= 0.5f;
                scale *= 2f;
            }
            int noiseInt = (int)MathF.Round(noiseAccum);
            noiseInt = MathHelper.Clamp(noiseInt, 0, 255);
            return (byte)noiseInt;
        }

        public byte PerlinNoise3D(float x, float y, float z)
        {
            int x0 = FastFloor(x);
            int y0 = FastFloor(y);
            int z0 = FastFloor(z);

            float dx = x - x0;
            float dy = y - y0;
            float dz = z - z0;

            byte n000 = (byte)PRNG(x0, y0, z0);
            byte n100 = (byte)PRNG(x0 + 1, y0, z0);
            byte n010 = (byte)PRNG(x0, y0 + 1, z0);
            byte n110 = (byte)PRNG(x0 + 1, y0 + 1, z0);
            byte n001 = (byte)PRNG(x0, y0, z0 + 1);
            byte n101 = (byte)PRNG(x0 + 1, y0, z0 + 1);
            byte n011 = (byte)PRNG(x0, y0 + 1, z0 + 1);
            byte n111 = (byte)PRNG(x0 + 1, y0 + 1, z0 + 1);

            byte n00 = Interpolate(n000, n100, dx);
            byte n10 = Interpolate(n010, n110, dx);
            byte n01 = Interpolate(n001, n101, dx);
            byte n11 = Interpolate(n011, n111, dx);

            byte n0 = Interpolate(n00, n10, dy);
            byte n1 = Interpolate(n01, n11, dy);

            return Interpolate(n0, n1, dz);
        }

        public byte PerlinNoise3DWithMask(Vector3 pos, ref Vector3 regionSize, byte octaves, Mask mask)
        {
            byte noiseValue = PerlinNoise3D(pos.X, pos.Y, pos.Z, octaves);
            if (mask != null)
            {
                noiseValue = mask(noiseValue, ref pos, ref regionSize);
            }
            return noiseValue;
        }

        public int PRNG(int x, int y, int z)
        {
            x = PRNGStep(x) + y;
            x = PRNGStep(x) + z;
            x = PRNGStep(x);
            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PRNGStep(int value)
        {
            value = (value << 13) ^ value;
            value = value * (value * value * baseSeed + 789221) + 87461947;
            return value & int.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float value)
        {
            return (value < 0) ? ((int)value - 1) : (int)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Interpolate(byte a, byte b, float t)
        {
            float ft = (1f - MathF.Cos(t * MathF.PI)) * 0.5f;
            return (byte)(a + (b - a) * ft);
        }

        public delegate byte Mask(byte noiseValue, ref Vector3 pos, ref Vector3 regionSize);
    }

}
