namespace Spacebox.Game.Player;

public sealed class LowOxygenBeepSchedule
{
    public const float SlowInterval = 2.8f;
    public const float FastInterval = 1.4f;

    private float _timer;
    private bool _active;

    public bool IsActive => _active;

    public static float Interval(float level)
    {
        float t = Math.Clamp(level, 0f, 1f);
        return SlowInterval + (FastInterval - SlowInterval) * t;
    }

    public bool Update(float level, float deltaTime)
    {
        if (level <= 0f)
        {
            Reset();
            return false;
        }

        if (!_active)
        {
            _active = true;
            _timer = Interval(level);
            return true;
        }

        _timer -= Math.Max(0f, deltaTime);
        if (_timer > 0f) return false;

        _timer = Interval(level);
        return true;
    }

    public void Reset()
    {
        _active = false;
        _timer = 0f;
    }
}
