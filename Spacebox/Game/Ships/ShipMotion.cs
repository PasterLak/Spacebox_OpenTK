using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class ShipMotion
{
    private Vector3 _force;
    private Vector3 _torque;
    private bool _hasForce;

    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; private set; } = 1f;
    public Vector3 Inertia { get; private set; } = Vector3.One;

    public bool IsMoving => Velocity != Vector3.Zero || AngularVelocity != Vector3.Zero;
    public bool HasPendingForce => _hasForce;

    public void SetMassProperties(float mass, Vector3 inertia)
    {
        Mass = Math.Max(0.001f, mass);
        Inertia = new Vector3(Math.Max(0.001f, inertia.X), Math.Max(0.001f, inertia.Y), Math.Max(0.001f, inertia.Z));
    }

    public void ApplyForce(Vector3 pointLocal, Vector3 forceLocal, Vector3 centerOfMassLocal)
    {
        _force += forceLocal;
        _torque += Vector3.Cross(pointLocal - centerOfMassLocal, forceLocal);
        _hasForce = true;
    }

    public void ApplyImpulse(Vector3 pointLocal, Vector3 impulseLocal, Vector3 centerOfMassLocal, Quaternion orientation)
    {
        Velocity += Vector3.Transform(impulseLocal, orientation) / Mass;
        AngularVelocity += Divide(Vector3.Cross(pointLocal - centerOfMassLocal, impulseLocal), Inertia);
    }

    public Vector3 GetPointVelocity(Vector3 pointLocal, Vector3 centerOfMassLocal, Quaternion orientation)
    {
        var angular = Vector3.Cross(AngularVelocity, pointLocal - centerOfMassLocal);
        return Velocity + Vector3.Transform(angular, orientation);
    }

    public Vector3 Step(float deltaTime, ref Quaternion orientation, ShipPhysicsSettings settings)
    {
        IntegrateVelocity(deltaTime, orientation, settings);
        return Advance(deltaTime, ref orientation);
    }

    public void IntegrateVelocity(float deltaTime, Quaternion orientation, ShipPhysicsSettings settings, float speedLimit = 0f)
    {
        if (_hasForce)
        {
            Velocity += Vector3.Transform(_force, orientation) / Mass * deltaTime;
            AngularVelocity += Divide(_torque, Inertia) * deltaTime;
        }
        else
        {
            Velocity = Damp(Velocity, settings.LinearDamping * deltaTime, settings.LinearSnap);
            AngularVelocity = Damp(AngularVelocity, settings.AngularDamping * deltaTime, settings.AngularSnap);
        }

        Velocity = Clamp(Velocity, speedLimit > 0f ? Math.Min(settings.MaxSpeed, speedLimit) : settings.MaxSpeed);
        AngularVelocity = Clamp(AngularVelocity, settings.MaxAngularSpeed);

        _force = Vector3.Zero;
        _torque = Vector3.Zero;
        _hasForce = false;
    }

    public Vector3 Advance(float deltaTime, ref Quaternion orientation)
    {
        float angularSpeed = AngularVelocity.Length;
        if (angularSpeed > 0f)
        {
            var delta = Quaternion.FromAxisAngle(AngularVelocity / angularSpeed, angularSpeed * deltaTime);
            orientation = Quaternion.Normalize(orientation * delta);
        }

        return Velocity * deltaTime;
    }

    public float GetInverseMassAlong(Vector3 armLocal, Vector3 normalLocal)
    {
        var c = Vector3.Cross(armLocal, normalLocal);
        return 1f / Mass + c.X * c.X / Inertia.X + c.Y * c.Y / Inertia.Y + c.Z * c.Z / Inertia.Z;
    }

    public void Stop()
    {
        Velocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        _force = Vector3.Zero;
        _torque = Vector3.Zero;
        _hasForce = false;
    }

    private static Vector3 Damp(Vector3 value, float amount, float snap)
    {
        float length = value.Length;
        if (length <= snap || length <= amount) return Vector3.Zero;
        return value - value / length * amount;
    }

    private static Vector3 Clamp(Vector3 value, float max)
    {
        float length = value.Length;
        return length > max ? value / length * max : value;
    }

    private static Vector3 Divide(Vector3 value, Vector3 divisor)
    {
        return new Vector3(value.X / divisor.X, value.Y / divisor.Y, value.Z / divisor.Z);
    }
}
