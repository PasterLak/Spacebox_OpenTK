using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public struct ShipMassProperties
{
    private const float CubeInertiaFactor = 1f / 6f;

    private float _mass;
    private Vector3 _weightedPosition;
    private Vector3 _secondMoment;
    private float _selfInertia;

    public float Mass => _mass;
    public Vector3 CenterOfMass => _mass > 0f ? _weightedPosition / _mass : Vector3.Zero;

    public void Add(Vector3 position, float mass)
    {
        if (mass <= 0f) return;

        _mass += mass;
        _weightedPosition += position * mass;
        _secondMoment += new Vector3(position.X * position.X, position.Y * position.Y, position.Z * position.Z) * mass;
        _selfInertia += mass * CubeInertiaFactor;
    }

    public Vector3 GetInertia()
    {
        if (_mass <= 0f) return Vector3.One;

        var com = CenterOfMass;
        var spread = _secondMoment - new Vector3(com.X * com.X, com.Y * com.Y, com.Z * com.Z) * _mass;
        return new Vector3(
            spread.Y + spread.Z + _selfInertia,
            spread.X + spread.Z + _selfInertia,
            spread.X + spread.Y + _selfInertia);
    }
}
