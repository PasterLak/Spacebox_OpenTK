using OpenTK.Mathematics;

namespace Spacebox.Game
{
    public class StatsBarData
    {
        public int Count { get; set; } = 0;
        public int MaxCount { get; set; } = 100;
        public string Name { get; set; } = "Default";

        public event Action DataChanged;

        public StatsBarData() 
        {
        }


        public void Increment(int amount)
        {

            if (Count == MaxCount) return;

            Count = MathHelper.Min(Count + amount, MaxCount);
            DataChanged?.Invoke();
        }

        public void Decrement(int amount)
        {

            if (Count == 0) return;

            Count = MathHelper.Max(Count - amount, 0);
            DataChanged?.Invoke();

        }
    }
}
