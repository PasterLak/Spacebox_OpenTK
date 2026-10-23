using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Spacebox.Game.Generation;
using Spacebox.Tests;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



namespace Spacebox.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Tests...");
          

           /* long before = GC.GetTotalMemory(true);
            var block = new Spacebox.Game.Generation.Block2();
            long after = GC.GetTotalMemory(true);

            Console.WriteLine($"Size of Block: {after - before} bytes");*/
            Console.WriteLine($"--------------------------------------------");
            if (args.Length > 0)
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));
                return;
            }
            var summary = BenchmarkRunner.Run<DownscaleBenchmarks>();
        }

        
    }
}
