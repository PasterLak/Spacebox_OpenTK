using OpenTK.Mathematics;

namespace Engine.Networking;

public sealed class TransformInterpolator
{
    private const float OffsetDrift = 0.01f;
    private const float ResetThreshold = 2f;

    private readonly TransformState[] _buffer;
    private int _start;
    private int _count;
    private float _offset;
    private bool _hasOffset;

    public TransformInterpolator(int capacity)
    {
        _buffer = new TransformState[Math.Max(2, capacity)];
    }

    public float ExpectedInterval { get; set; } = 0.05f;
    public float SnapDistance { get; set; } = 20f;
    public int Count => _count;
    public float ClockOffset => _offset;

    public void Clear()
    {
        _start = 0;
        _count = 0;
        _hasOffset = false;
    }

    public void Add(float localTime, TransformState state)
    {
        float sample = localTime - state.Time;

        if (_count > 0)
        {
            var latest = Latest;
            bool clockJumped = state.Time < latest.Time - ResetThreshold || MathF.Abs(sample - _offset) > ResetThreshold;
            if (clockJumped || Vector3.DistanceSquared(latest.Position, state.Position) > SnapDistance * SnapDistance)
            {
                Clear();
            }
            else
            {
                if (state.Time <= latest.Time) return;

                float gap = state.Time - latest.Time;
                if (gap > ExpectedInterval * 2.5f)
                    Push(new TransformState(state.Time - ExpectedInterval, latest.Position, latest.Rotation));
            }
        }

        if (!_hasOffset || sample < _offset)
        {
            _offset = sample;
            _hasOffset = true;
        }
        else
        {
            _offset += (sample - _offset) * OffsetDrift;
        }

        Push(state);
    }

    public bool TrySample(float localTime, float delay, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.Zero;
        rotation = Quaternion.Identity;
        if (_count == 0) return false;

        float time = localTime - _offset - delay;
        var first = Get(0);
        if (_count == 1 || time <= first.Time)
        {
            position = first.Position;
            rotation = first.Rotation;
            return true;
        }

        for (int i = 1; i < _count; i++)
        {
            var next = Get(i);
            if (time > next.Time) continue;

            var previous = Get(i - 1);
            float span = MathF.Max(next.Time - previous.Time, 1e-4f);
            float t = Math.Clamp((time - previous.Time) / span, 0f, 1f);
            position = Vector3.Lerp(previous.Position, next.Position, t);
            rotation = Quaternion.Slerp(previous.Rotation, next.Rotation, t);
            DropBefore(i - 1);
            return true;
        }

        var latest = Latest;
        position = latest.Position;
        rotation = latest.Rotation;
        DropBefore(_count - 1);
        return true;
    }

    private TransformState Latest => Get(_count - 1);

    private TransformState Get(int index)
    {
        return _buffer[(_start + index) % _buffer.Length];
    }

    private void Push(TransformState state)
    {
        if (_count == _buffer.Length)
        {
            _start = (_start + 1) % _buffer.Length;
            _count--;
        }

        _buffer[(_start + _count) % _buffer.Length] = state;
        _count++;
    }

    private void DropBefore(int index)
    {
        if (index <= 0) return;

        _start = (_start + index) % _buffer.Length;
        _count -= index;
    }
}
