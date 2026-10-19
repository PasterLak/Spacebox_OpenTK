using OpenTK.Mathematics;

namespace Spacebox.Game.Player;

public sealed class DistanceTracker
{
    public const float MaxPlausibleSpeed = 1000f;
    public const float MinStep = 0.001f;

    private Vector3 _last;
    private bool _hasLast;

    public float LastSpeed { get; private set; }

    public void Reset(Vector3 position)
    {
        LastSpeed = 0f;
        if (!IsFinite(position))
        {
            _hasLast = false;
            return;
        }

        _last = position;
        _hasLast = true;
    }

    public void Invalidate()
    {
        _hasLast = false;
        LastSpeed = 0f;
    }

    public double Step(Vector3 position, float deltaTime)
    {
        if (!IsFinite(position))
        {
            Invalidate();
            return 0;
        }

        if (!_hasLast)
        {
            Reset(position);
            return 0;
        }

        float step = Vector3.Distance(position, _last);
        _last = position;
        LastSpeed = 0f;

        if (!float.IsFinite(step) || step < MinStep) return 0;
        if (!float.IsFinite(deltaTime) || deltaTime <= 0f) return 0;

        float speed = step / deltaTime;
        if (speed > MaxPlausibleSpeed) return 0;

        LastSpeed = speed;
        return step;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }
}
