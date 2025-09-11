using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.Player
{
    public class StatsBarData
    {
        public int Value { get; set; } = 0;
        public int MaxValue { get; set; } = 100;
        public string Name { get; set; } = "Default";

        public event Action DataChanged;
        public event Action<int> OnDecrement;
        public event Action<int> OnIncrement;
        public event Action OnEqualZero;

        public bool IsMaxReached => Value >= MaxValue;
        public bool IsMinReached => Value <= 0;

        public void Increment(int amount)
        {
            if (IsMaxReached) return;

            amount = MathHelper.Abs(amount);

            int oldValue = Value;
            Value = Math.Min(Value + amount, MaxValue);
            int actualIncrement = Value - oldValue;
            DataChanged?.Invoke();
            OnIncrement?.Invoke(actualIncrement);
        }

        public void Decrement(int amount)
        {
            amount = MathHelper.Abs(amount);
            int oldValue = Value;
            Value = Math.Max(Value - amount, 0);
            int actualDecrement = oldValue - Value;
            if (IsMinReached)
            {
                OnEqualZero?.Invoke();
            }
            DataChanged?.Invoke();
            OnDecrement?.Invoke(actualDecrement);
        }
    }
}