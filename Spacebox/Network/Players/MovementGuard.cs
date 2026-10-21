using OpenTK.Mathematics;

namespace Spacebox.Network.Players;

public sealed class MovementGuard
{
    public const float MaxSpeed = 60f;
    public const float Tolerance = 10f;
    public const float ClockSlack = 2f;

    private double _graceUntil = double.MinValue;
    private double _lastServerTime;
    private float _lastOwnerTime;
    private bool _hasLast;

    public Vector3 LastPosition { get; private set; }

    public void AllowJump(double now, double seconds)
    {
        _graceUntil = Math.Max(_graceUntil, now + seconds);
    }

    public bool Accept(Vector3 position, float ownerTime, double now)
    {
        if (!NetValidation.IsFinite(position) || !float.IsFinite(ownerTime)) return false;

        if (_hasLast && now >= _graceUntil)
        {
            double serverElapsed = Math.Max(0.0, now - _lastServerTime);
            double ownerElapsed = Math.Max(0.0, ownerTime - _lastOwnerTime);
            double elapsed = Math.Min(Math.Max(ownerElapsed, serverElapsed), serverElapsed + ClockSlack);
            double allowed = MaxSpeed * elapsed + Tolerance;
            if (Vector3.DistanceSquared(position, LastPosition) > allowed * allowed) return false;
        }

        _hasLast = true;
        LastPosition = position;
        _lastServerTime = now;
        _lastOwnerTime = ownerTime;
        return true;
    }
}
