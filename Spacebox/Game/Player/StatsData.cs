using System;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class StatsData
    {
        private int _value = 0;
        private int _maxValue = 100;
        private string _name = "Default";

        public int Value
        {
            get => _value;
            set
            {
                int clampedValue = Math.Clamp(value, 0, _maxValue);
                if (_value == clampedValue) return;

                int oldValue = _value;
                _value = clampedValue;

                OnValueChanged?.Invoke();

                if (_value == _maxValue && oldValue != _maxValue)
                    OnMaxReached?.Invoke();

                if (_value == 0 && oldValue != 0)
                    OnEqualZero?.Invoke();
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                if (value < 0) value = 0;

                _maxValue = value;
                if (_value > _maxValue)
                    Value = _maxValue;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value ?? "Default";
        }

        public event Action OnValueChanged;
        public event Action<int> OnDecrement;
        public event Action<int> OnIncrement;
        public event Action OnEqualZero;
        public event Action OnMaxReached;

        public bool IsMaxReached => _value >= _maxValue;
        public bool IsMinReached => _value <= 0;
        public float Percentage => _maxValue > 0 ? (float)_value / _maxValue : 0f;

        public StatsData() { }

        public StatsData(string name, int maxValue, int initialValue = 0)
        {
            _name = name ?? "Default";
            _maxValue = Math.Max(0, maxValue);
            _value = Math.Clamp(initialValue, 0, _maxValue);
        }

        public void Increment(int amount)
        {
            if (amount <= 0 || IsMaxReached) return;

            int oldValue = _value;
            int targetValue = Math.Min(_value + amount, _maxValue);
            int actualIncrement = targetValue - oldValue;

            if (actualIncrement > 0)
            {
                Value = targetValue;
                OnIncrement?.Invoke(actualIncrement);
            }
        }

        public void Decrement(int amount)
        {
            if (amount <= 0 || IsMinReached) return;

            int oldValue = _value;
            int targetValue = Math.Max(_value - amount, 0);
            int actualDecrement = oldValue - targetValue;

            if (actualDecrement > 0)
            {
                Value = targetValue;
                OnDecrement?.Invoke(actualDecrement);
            }
        }

        public void SetValue(int newValue)
        {
            Value = newValue;
        }

        public void Reset()
        {
            Value = 0;
        }

        public void Fill()
        {
            Value = _maxValue;
        }

        public override string ToString()
        {
            return $"{_name}: {_value}/{_maxValue} ({Percentage:P0})";
        }
    }
}