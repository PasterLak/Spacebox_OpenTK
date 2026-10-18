using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipMotionTests
{
    private static readonly ShipPhysicsSettings Settings = new ShipPhysicsSettings();

    private static ShipMotion CreateMotion(float mass = 10f, float inertia = 10f)
    {
        var motion = new ShipMotion();
        motion.SetMassProperties(mass, new Vector3(inertia));
        return motion;
    }

    [Fact]
    public void CentralForce_AcceleratesWithoutRotation()
    {
        var motion = CreateMotion();
        var orientation = Quaternion.Identity;

        motion.ApplyForce(Vector3.Zero, new Vector3(0, 0, 20), Vector3.Zero);
        var displacement = motion.Step(0.5f, ref orientation, Settings);

        Assert.Equal(new Vector3(0, 0, 1), motion.Velocity);
        Assert.Equal(Vector3.Zero, motion.AngularVelocity);
        Assert.Equal(new Vector3(0, 0, 0.5f), displacement);
    }

    [Fact]
    public void OffCenterForce_CreatesTorque()
    {
        var motion = CreateMotion();
        var orientation = Quaternion.Identity;

        motion.ApplyForce(new Vector3(2, 0, 0), new Vector3(0, 0, 10), Vector3.Zero);
        motion.Step(0.1f, ref orientation, Settings);

        Assert.True(motion.AngularVelocity.Y < 0f);
        Assert.NotEqual(Quaternion.Identity, orientation);
    }

    [Fact]
    public void ForceIsInLocalSpace_RotatedByOrientation()
    {
        var motion = CreateMotion(mass: 1f);
        var orientation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.PiOver2);

        motion.ApplyForce(Vector3.Zero, Vector3.UnitZ, Vector3.Zero);
        motion.Step(1f, ref orientation, Settings);

        Assert.Equal(1f, motion.Velocity.X, 4);
        Assert.Equal(0f, motion.Velocity.Z, 4);
    }

    [Fact]
    public void WithoutThrust_ShipSlowsDownAndStops()
    {
        var motion = CreateMotion();
        motion.Velocity = new Vector3(1, 0, 0);
        var orientation = Quaternion.Identity;

        motion.Step(1f, ref orientation, Settings);
        Assert.Equal(0.75f, motion.Velocity.X, 4);

        for (int i = 0; i < 10; i++) motion.Step(1f, ref orientation, Settings);
        Assert.Equal(Vector3.Zero, motion.Velocity);
        Assert.False(motion.IsMoving);
    }

    [Fact]
    public void SpeedsAreCapped()
    {
        var motion = CreateMotion(mass: 1f, inertia: 0.01f);
        var orientation = Quaternion.Identity;

        motion.ApplyForce(new Vector3(5, 0, 0), new Vector3(0, 0, 10000), Vector3.Zero);
        motion.Step(1f, ref orientation, Settings);

        Assert.Equal(Settings.MaxSpeed, motion.Velocity.Length, 3);
        Assert.Equal(Settings.MaxAngularSpeed, motion.AngularVelocity.Length, 3);
    }

    [Fact]
    public void PointVelocity_IncludesRotation()
    {
        var motion = CreateMotion();
        motion.Velocity = new Vector3(1, 0, 0);
        motion.AngularVelocity = new Vector3(0, 1, 0);

        var velocity = motion.GetPointVelocity(new Vector3(0, 0, 2), Vector3.Zero, Quaternion.Identity);

        Assert.Equal(new Vector3(3, 0, 0), velocity);
    }

    [Fact]
    public void Impulse_ChangesVelocityImmediately()
    {
        var motion = CreateMotion(mass: 2f);

        motion.ApplyImpulse(Vector3.Zero, new Vector3(4, 0, 0), Vector3.Zero, Quaternion.Identity);

        Assert.Equal(new Vector3(2, 0, 0), motion.Velocity);
    }

    [Fact]
    public void MassProperties_ComputeCenterAndParallelAxisInertia()
    {
        var properties = new ShipMassProperties();
        properties.Add(new Vector3(-1, 0, 0), 1f);
        properties.Add(new Vector3(1, 0, 0), 1f);

        var inertia = properties.GetInertia();

        Assert.Equal(2f, properties.Mass);
        Assert.Equal(Vector3.Zero, properties.CenterOfMass);
        Assert.Equal(2f / 6f, inertia.X, 4);
        Assert.Equal(2f + 2f / 6f, inertia.Y, 4);
        Assert.Equal(2f + 2f / 6f, inertia.Z, 4);
    }

    [Fact]
    public void Settings_ValidateFixesBadValues()
    {
        var settings = new ShipPhysicsSettings { MaxSpeed = -1, LeaveDistance = 1, BoardDistance = 3, MaxStep = 5 };

        settings.Validate();

        Assert.True(settings.MaxSpeed > 0);
        Assert.True(settings.LeaveDistance > settings.BoardDistance);
        Assert.True(settings.MaxStep <= 0.25f);
    }
}
