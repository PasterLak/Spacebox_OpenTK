using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class StatsBarData
    {
        public int Count { get; set; } = 0;
        public int MaxCount { get; set; } = 100;
        public string Name { get; set; } = "Default";

        public event Action DataChanged;
        public event Action OnEqualZero;

        public bool IsMaxReached => Count >= MaxCount;
        public bool IsMinReached => Count <= 0;

        public void Increment(int amount)
        {
            if (Count >= MaxCount) return;

            amount = MathHelper.Abs(amount);

            Count = Math.Min(Count + amount, MaxCount);
            DataChanged?.Invoke();
        }

        public void Decrement(int amount)
        {
            if (Count <= 0) return;
            amount = MathHelper.Abs(amount);

            Count = Math.Max(Count - amount, 0);

            if (Count == 0)
            {
                OnEqualZero?.Invoke();
            }

            DataChanged?.Invoke();
        }
    }
}