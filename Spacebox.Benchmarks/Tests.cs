using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Benchmarks
{
    //[ShortRunJob]
    //[SimpleJob(iterationCount: 100, warmupCount: 10)]
    public class MyConfig : ManualConfig
    {
        public MyConfig()
        {
            AddJob(Job.ShortRun
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(15));
           // AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    //[Config(typeof(MyConfig))]
    [MemoryDiagnoser]
    public class Tests
    {


        public static IEnumerable<Vector3> NormalVectors => new List<Vector3>
    {
        new Vector3(1f, 0f, 0f)
    };

        [ParamsSource(nameof(NormalVectors))]
        public Vector3 testNormal;

        [Benchmark]
        public void OldImplementation()
        {
            
                var direction = Spacebox.Game.Generation.Block.GetDirectionFromNormal(testNormal);
            
        }

        [Benchmark]
        public void NewImplementation()
        {
        
                var direction = Spacebox.Game.Generation.Block.GetDirectionFromNormal(testNormal);
            
        }

    }

    
}
