using OpenTK.Mathematics;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Tests.ShipViewer;

public class ShipViewOrientationTests
{
    [Fact]
    public void NoConsole_KeepsEntityAxes()
    {
        Assert.Equal(Matrix3.Identity, new ShipViewOrientation().Resolve());
    }

    [Fact]
    public void Console_MapsPilotUpToViewUp()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitX, Vector3.UnitZ, true);

        var rotation = orientation.Resolve();

        AssertClose(Vector3.UnitY, Vector3.TransformRow(Vector3.UnitZ, rotation));
        AssertClose(Vector3.UnitZ, Vector3.TransformRow(Vector3.UnitX, rotation));
        AssertClose(Vector3.UnitX, Vector3.TransformRow(Vector3.UnitY, rotation));
    }

    [Fact]
    public void FlightConsoles_WinOverOtherConsoles()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitZ, Vector3.UnitY, false);
        orientation.AddConsole(Vector3.UnitZ, Vector3.UnitY, false);
        orientation.AddConsole(-Vector3.UnitY, Vector3.UnitX, true);

        var rotation = orientation.Resolve();

        AssertClose(Vector3.UnitY, Vector3.TransformRow(Vector3.UnitX, rotation));
    }

    [Fact]
    public void MostCommonConsoleFrameWins()
    {
        var orientation = new ShipViewOrientation();
        orientation.AddConsole(Vector3.UnitX, -Vector3.UnitY, true);
        orientation.AddConsole(Vector3.UnitZ, Vector3.UnitX, true);
        orientation.AddConsole(Vector3.UnitX, -Vector3.UnitY, true);

        var rotation = orientation.Resolve();

        AssertClose(Vector3.UnitY, Vector3.TransformRow(-Vector3.UnitY, rotation));
    }

    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.True((expected - actual).Length < 1e-4f, $"expected {expected}, got {actual}");
    }
}
