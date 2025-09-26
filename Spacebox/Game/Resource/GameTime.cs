using Engine;
namespace Spacebox.Game.Resource
{
    public static class GameTime
    {
        public static int Day { get; private set; } = 0;
        public static int DayTick { get; private set; } = 0;
        public static int Hour { get; private set; } = 0;
        public static int Minute { get; private set; } = 0;
        public static Action OnTimeChanged;
        public static Action OnDayChanged;
        private const int TicksPerDay = 24000;
        private const int TicksPerHour = TicksPerDay / 24;

        public static void Init()
        {
            Day = 0;
            DayTick = 0;
            CalculateHourAndMinute();
            Time.OnTick += OnTick;
        }

        public new static string ToString()
        {
            return $"Day {Day}, {Hour:D2}:{Minute:D2}";
        }

        private static void OnTick()
        {
            DayTick++;
            CalculateHourAndMinute();
            OnTimeChanged?.Invoke();
            if (DayTick >= TicksPerDay)
            {
                DayTick = 0;
                Day++;
                CalculateHourAndMinute();
                OnDayChanged?.Invoke();
            }
        }

        public static void SetDay(int day)
        {
            if (day < 0) day = 0;
            Day = day;
            OnDayChanged?.Invoke();
        }

        public static void SetTick(int tick)
        {
            DayTick = Math.Clamp(tick, 0, TicksPerDay - 1);
            CalculateHourAndMinute();
            OnTimeChanged?.Invoke();
        }

        public static void SetTime(int day, int hour, int minute)
        {
            if (day < 0) day = 0;
            hour = Math.Clamp(hour, 0, 23);
            minute = Math.Clamp(minute, 0, 59);

            Day = day;
            DayTick = hour * TicksPerHour + (minute * TicksPerHour) / 60;

            CalculateHourAndMinute();
            OnTimeChanged?.Invoke();
            OnDayChanged?.Invoke();
        }

        private static void CalculateHourAndMinute()
        {
            Hour = DayTick / TicksPerHour;
            Minute = (DayTick % TicksPerHour * 60) / TicksPerHour;
        }

        public static float GetDayProgress()
        {
            return (float)DayTick / TicksPerDay;
        }

        public static bool IsNight()
        {
            return Hour >= 22 || Hour < 6;
        }

        public static void Dispose()
        {
            Time.OnTick -= OnTick;
            OnTimeChanged = null;
            OnDayChanged = null;
        }
    }
}