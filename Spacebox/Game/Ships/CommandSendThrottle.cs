using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class CommandSendThrottle
{
    public const float MinInterval = 0.05f;
    public const float KeepAliveInterval = 0.25f;
    public const float AimThreshold = 0.002f;

    private ConsoleCommand _last;
    private float _lastTime = float.MinValue;
    private bool _hasLast;

    public bool ShouldSend(in ConsoleCommand command, float now)
    {
        float elapsed = now - _lastTime;
        bool inputChanged = !_hasLast || command.Buttons != _last.Buttons || command.Flight != _last.Flight;
        bool aimChanged = _hasLast && (Vector3.DistanceSquared(command.AimLocal, _last.AimLocal) > AimThreshold * AimThreshold ||
                                       Vector3.DistanceSquared(command.AimPointLocal, _last.AimPointLocal) > AimThreshold * AimThreshold);

        if (!inputChanged && !(aimChanged && elapsed >= MinInterval) && elapsed < KeepAliveInterval) return false;

        _last = command;
        _lastTime = now;
        _hasLast = true;
        return true;
    }

    public void Reset() => _hasLast = false;
}
