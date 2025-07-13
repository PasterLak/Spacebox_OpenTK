using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Spacebox.Game.Generation;

namespace Spacebox.Benchmarks
{
    [MemoryDiagnoser]
    public class DownscaleBenchmarks
    {
        [Params(16, 32)]
        public int Orig;

        [Params(1, 2, 4)]
        public int Downscale;

        private bool[,,] data;
        private MethodInfo oldM, newM, optM;

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(42);
            data = new bool[Orig, Orig, Orig];
            for (int x = 0; x < Orig; x++)
                for (int y = 0; y < Orig; y++)
                    for (int z = 0; z < Orig; z++)
                        data[x, y, z] = r.NextDouble() < 0.5;
            var t = typeof(ChunkLODMeshGenerator);
            oldM = t.GetMethod("CreateDownscaledData", BindingFlags.NonPublic | BindingFlags.Static);
            newM = t.GetMethod("CreateDownscaledData2", BindingFlags.NonPublic | BindingFlags.Static);
            optM = t.GetMethod("CreateDownscaledData3", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Benchmark(Baseline = true)]
        public bool[,,] Old() =>
            (bool[,,])oldM.Invoke(null, new object[] { data, Downscale });

        [Benchmark]
        public bool[,,] New() =>
            (bool[,,])newM.Invoke(null, new object[] { data, Downscale });

        [Benchmark]
        public bool[,,] Optimized() =>
            (bool[,,])optM.Invoke(null, new object[] { data, Downscale });
    }

}
