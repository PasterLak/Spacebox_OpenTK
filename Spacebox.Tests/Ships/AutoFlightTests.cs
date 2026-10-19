using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class AutoFlightTests
{
    private static readonly ConsoleFrame Frame = new ConsoleFrame(-Vector3.UnitZ, Vector3.UnitY);

    [Fact]
    public void Classify_CentralThrusters_MapToTranslation()
    {
        Assert.Equal(FlightInput.Forward, AutoFlight.Classify(-Vector3.UnitZ, Vector3.Zero, Vector3.Zero, Frame));
        Assert.Equal(FlightInput.Backward, AutoFlight.Classify(Vector3.UnitZ, Vector3.Zero, Vector3.Zero, Frame));
        Assert.Equal(FlightInput.Up, AutoFlight.Classify(Vector3.UnitY, Vector3.Zero, Vector3.Zero, Frame));
        Assert.Equal(FlightInput.Left, AutoFlight.Classify(-Vector3.UnitX, Vector3.Zero, Vector3.Zero, Frame));
    }

    [Fact]
    public void Classify_OffCenterThruster_AddsYaw()
    {
        var flags = AutoFlight.Classify(-Vector3.UnitZ, new Vector3(2, 0, 0), Vector3.Zero, Frame);

        Assert.True(flags.HasFlag(FlightInput.Forward));
        Assert.True(flags.HasFlag(FlightInput.YawLeft));
    }

    [Fact]
    public void ShouldFire_BrakeUsesThrustersAgainstVelocity()
    {
        var brake = AutoFlight.BrakeDirection(new Vector3(0, 0, -5));

        Assert.True(AutoFlight.ShouldFire(FlightInput.Backward, FlightInput.Brake, Vector3.UnitZ, brake));
        Assert.False(AutoFlight.ShouldFire(FlightInput.Forward, FlightInput.Brake, -Vector3.UnitZ, brake));
    }

    [Fact]
    public void ShouldFire_BrakeIgnoredWhileTranslating()
    {
        var brake = AutoFlight.BrakeDirection(new Vector3(0, 0, -5));

        Assert.False(AutoFlight.ShouldFire(FlightInput.Backward, FlightInput.Brake | FlightInput.Left, Vector3.UnitZ, brake));
    }

    [Fact]
    public void BrakeDirection_ZeroWhenStill()
    {
        Assert.Equal(Vector3.Zero, AutoFlight.BrakeDirection(new Vector3(0.01f, 0, 0)));
    }

    [Fact]
    public void Steer_AimLeft_YawsLeft()
    {
        var flags = AutoFlight.Steer(new Vector3(-0.3f, 0, -1), Vector3.Zero, Frame);

        Assert.Equal(FlightInput.YawLeft, flags);
    }

    [Fact]
    public void Steer_AlreadyTurningFastEnough_DoesNothing()
    {
        var aim = new Vector3(-0.3f, 0, -1).Normalized();
        float angle = ShipAiming.AngleBetween(-Vector3.UnitZ, aim);

        var flags = AutoFlight.Steer(aim, new Vector3(0, angle * 2f, 0), Frame);

        Assert.Equal(FlightInput.None, flags);
    }

    [Fact]
    public void Steer_OnTarget_DoesNothing()
    {
        Assert.Equal(FlightInput.None, AutoFlight.Steer(-Vector3.UnitZ, Vector3.Zero, Frame));
    }

    private static List<bool> Select(params Vector3[] forces)
    {
        var selected = forces.Select(_ => true).ToList();
        AutoFlight.SelectBalanced(forces.ToList(), selected);
        return selected;
    }

    [Fact]
    public void SelectBalanced_SingleThrusterIsDropped()
    {
        Assert.Equal(new[] { false }, Select(new Vector3(0, 0, -10)));
    }

    [Fact]
    public void SelectBalanced_OpposedPairFires()
    {
        Assert.Equal(new[] { true, true }, Select(new Vector3(-10, 0, 0), new Vector3(10, 0, 0)));
    }

    [Fact]
    public void SelectBalanced_DropsUnmatchedExtraThruster()
    {
        var selected = Select(new Vector3(-10, 0, 0), new Vector3(10, 0, 0), new Vector3(0, 0, -10));

        Assert.Equal(new[] { true, true, false }, selected);
    }

    [Fact]
    public void SelectBalanced_AcceptsSmallImbalance()
    {
        Assert.Equal(new[] { true, true }, Select(new Vector3(-10, 0, 0), new Vector3(9, 0, 0)));
    }

    [Fact]
    public void SelectBalanced_EmptyListStaysEmpty()
    {
        var selected = new List<bool>();
        AutoFlight.SelectBalanced(new List<Vector3>(), selected);

        Assert.Empty(selected);
    }
}
