using OpenTK.Mathematics;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Tests.ShipViewer;

public class ShipViewOrientationTests
{
    [Fact]
    public void ConsoleTopAndMainThrust_TurnShipUpright()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(-Vector3.UnitZ, true);
        for (int i = 0; i < 10; i++) orientation.AddThruster(-Vector3.UnitX, 100f);
        orientation.AddThruster(Vector3.UnitY, 100f);
        orientation.AddCannon(-Vector3.UnitX);

        var rotation = orientation.Resolve(new Vector3(31f, 27f, 8f));

        AssertClose(Vector3.UnitY, Vector3.TransformRow(-Vector3.UnitZ, rotation));
        AssertClose(Vector3.UnitZ, Vector3.TransformRow(-Vector3.UnitX, rotation));
    }

    [Fact]
    public void ThrustAlongUp_IsIgnoredForForward()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitY, true);
        orientation.AddThruster(Vector3.UnitY, 1000f);
        orientation.AddThruster(Vector3.UnitZ, 10f);

        var rotation = orientation.Resolve(Vector3.One);

        AssertClose(Vector3.UnitZ, Vector3.TransformRow(Vector3.UnitZ, rotation));
        AssertClose(Vector3.UnitY, Vector3.TransformRow(Vector3.UnitY, rotation));
    }

    [Fact]
    public void FlightConsoles_WinOverCannonConsoles()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitY, false);
        orientation.AddConsole(Vector3.UnitY, false);
        orientation.AddConsole(Vector3.UnitX, true);

        var rotation = orientation.Resolve(Vector3.One);

        AssertClose(Vector3.UnitY, Vector3.TransformRow(Vector3.UnitX, rotation));
    }

    [Fact]
    public void CannonsGiveForward_WithoutThrusters()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitY, true);
        orientation.AddCannon(Vector3.UnitX);

        var rotation = orientation.Resolve(Vector3.One);

        AssertClose(Vector3.UnitZ, Vector3.TransformRow(Vector3.UnitX, rotation));
    }

    [Fact]
    public void NoParts_UseThinnestAxisAsUpAndLongestAsForward()
    {
        var rotation = new ShipViewOrientation().Resolve(new Vector3(10f, 30f, 4f));

        AssertClose(Vector3.UnitY, Vector3.TransformRow(Vector3.UnitZ, rotation));
        AssertClose(Vector3.UnitZ, Vector3.TransformRow(Vector3.UnitY, rotation));
    }

    [Fact]
    public void Rotation_IsProper()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(-Vector3.UnitX, true);
        orientation.AddThruster(Vector3.UnitZ, 1f);

        Assert.Equal(1f, orientation.Resolve(Vector3.One).Determinant, 4);
    }

    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.True((expected - actual).Length < 1e-4f, $"expected {expected}, got {actual}");
    }
}
