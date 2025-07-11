
using BenchmarkDotNet.Attributes;
using Spacebox.Generation;

namespace Spacebox.Benchmarks
{
    [MemoryDiagnoser]
    public class InternalCavitiesBenchmark
    {
        private bool[,,] _original;
        private const int SX = 32;
        private Random _rnd;

        [GlobalSetup]
        public void Setup()
        {
            _rnd = new Random(12345);
            _original = new bool[SX, SX, SX];
            for (int x = 0; x < SX; x++)
                for (int y = 0; y < SX; y++)
                    for (int z = 0; z < SX; z++)
                        _original[x, y, z] = _rnd.NextDouble() < 0.5;
        }

        private static bool[,,] Clone(bool[,,] src)
        {
            int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
            var dst = new bool[sx, sy, sz];
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        dst[x, y, z] = src[x, y, z];
            return dst;
        }

        [Benchmark]
        public void UnityVersion()
        {
            var data = Clone(_original);
            InternalCavitiesUnity.RemoveInternalCavities(ref data);
        }

        [Benchmark]
        public void BitwiseVersion()
        {
            var data = Clone(_original);
            InternalCavitiesBits.FillInternalCavities(ref data);
        }
    }
}
