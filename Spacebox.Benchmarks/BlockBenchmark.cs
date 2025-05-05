using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Tests
{
    [MemoryDiagnoser]
    public class BlockBenchmark
    {
        private Block2 _blockPacked;
        private Block _blockOld;

        [GlobalSetup]
        public void Setup()
        {
           
            _blockPacked = new Block2(1, Direction2.Up, 100, 50, false);
            _blockOld = new Block();
        }

        [Benchmark]
        public void New_BlockOperations()
        {
            long sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += _blockPacked.Mass;
                sum += _blockPacked.Health;
            }

            int count = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (_blockPacked.IsTransparent) count++;
                if (_blockPacked.IsAir) count--;
                if (_blockPacked.IsLight) count++;
            }

            for (int i = 0; i < 1000; i++)
            {
                if (_blockPacked.Direction == Direction2.Up)
                    count++;
                else
                    count--;
            }
        }

        [Benchmark]
        public void Old_BlockOperations()
        {
            long sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += _blockOld.Mass;
                sum += _blockOld.Durability;
            }

            int count = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (_blockOld.IsTransparent) count++;
                if (_blockOld.IsAir) count--;
                if (_blockOld.IsLight) count++;
            }

            for (int i = 0; i < 1000; i++)
            {
                if (_blockOld.Direction == Direction.Up)
                    count++;
                else
                    count--;
            }
        }
    }

}
