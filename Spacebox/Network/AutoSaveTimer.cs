namespace Spacebox.Network;

public sealed class AutoSaveTimer
{
    public const float MinInterval = 10f;

    private float _elapsed;

    public float Elapsed => _elapsed;

    public static float Normalize(float interval)
    {
        return interval <= 0f ? 0f : Math.Max(MinInterval, interval);
    }

    public bool Tick(float deltaTime, float interval)
    {
        interval = Normalize(interval);
        if (interval <= 0f)
        {
            _elapsed = 0f;
            return false;
        }

        _elapsed += Math.Max(0f, deltaTime);
        if (_elapsed < interval) return false;

        _elapsed = 0f;
        return true;
    }

    public void Reset()
    {
        _elapsed = 0f;
    }
}
