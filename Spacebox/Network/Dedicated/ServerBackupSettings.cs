namespace Spacebox.Network.Dedicated;

public sealed class ServerBackupSettings
{
    public const float MinIntervalMinutes = 10f;
    public const float MaxIntervalMinutes = 7 * 24 * 60f;
    public const int MaxKeepLast = 1000;

    public bool Enabled { get; set; } = true;
    public float IntervalMinutes { get; set; } = 360f;
    public int KeepLast { get; set; } = 10;
    public bool OnStop { get; set; } = true;

    public float IntervalSeconds => Math.Max(MinIntervalMinutes, IntervalMinutes) * 60f;

    public bool Validate()
    {
        bool corrected = false;
        if (!float.IsFinite(IntervalMinutes) || IntervalMinutes < MinIntervalMinutes || IntervalMinutes > MaxIntervalMinutes)
        {
            IntervalMinutes = float.IsFinite(IntervalMinutes) ? Math.Clamp(IntervalMinutes, MinIntervalMinutes, MaxIntervalMinutes) : 360f;
            corrected = true;
        }
        if (KeepLast < 0 || KeepLast > MaxKeepLast)
        {
            KeepLast = Math.Clamp(KeepLast, 0, MaxKeepLast);
            corrected = true;
        }
        return corrected;
    }
}
