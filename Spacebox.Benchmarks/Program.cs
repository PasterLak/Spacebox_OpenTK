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
            var summary = BenchmarkRunner.Run<DownscaleBenchmarks>();
        }

        
    }
}
