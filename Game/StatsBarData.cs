using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class StatsBarData
    {
        public int Count { get; set; } = 0;
        public int MaxCount { get; set; } = 100;
        public string Name { get; set; } = "Default";
        public StatsBarData() 
        {
        }

        public StatsBarData(int min)
        {
        }

        public void Add(int count)
        {

            if (Count == MaxCount) return;

            Count = MathHelper.Min(Count + count, MaxCount);

        }

        public void Remove(int count)
        {

            if (Count == 0) return;

            Count = MathHelper.Max(Count - count, 0);

        }
    }
}
