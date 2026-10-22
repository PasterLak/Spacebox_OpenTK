namespace Spacebox.Network.Dedicated;

public sealed class ServerBackupSettings
{
    public const float MinIntervalMinutes = 10f;

    public bool Enabled { get; set; } = true;
    public float IntervalMinutes { get; set; } = 360f;
    public int KeepLast { get; set; } = 10;
    public bool OnStop { get; set; } = true;

    public float IntervalSeconds => Math.Max(MinIntervalMinutes, IntervalMinutes) * 60f;
}
