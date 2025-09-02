using Engine;

namespace Spacebox.Game.Resource
{
    public class GameTime
    {
        public static int Day { get; private set; } = 0;
        public static int DayTick { get; private set; } = 0;
        public static int Hour { get; private set; } = 0;
        public static int Minute { get; private set; } = 0;

        public static Action OnTimeChanged;
        public static Action OnDayChanged;

        private const int TicksPerDay = 24000;
        public static void Init()
        {
            Day = 0;
            DayTick = 0;

            Time.OnTick += OnTick;
        }

        public new static string ToString()
        {
            return $"Day {Day}, {Hour:D2}:{Minute:D2}";
        }

        private static void OnTick()
        {
            DayTick++;

            Hour = (DayTick / 1000 + 6) % 24;
            Minute = (int)((DayTick % 1000) * 60 / 1000);

            OnTimeChanged?.Invoke();

            if (DayTick >= TicksPerDay)
            {
                DayTick = 0;
                Day++;
                OnDayChanged?.Invoke();
            }
        }

        public static void Dispose()
        {
            Time.OnTick -= OnTick;
            OnTimeChanged = null;
            OnDayChanged = null;
        }
    }
}
