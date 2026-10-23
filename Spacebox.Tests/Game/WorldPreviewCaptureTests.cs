using Spacebox.Game.Rendering;

namespace Spacebox.Tests.Game;

public class WorldPreviewCaptureTests
{
    [Fact]
    public void Run_IsActiveOnlyDuringTheCapture()
    {
        bool during = false;

        WorldPreviewCapture.Run(() => during = WorldPreviewCapture.IsActive);

        Assert.True(during);
        Assert.False(WorldPreviewCapture.IsActive);
    }

    [Fact]
    public void Run_EndsTheCaptureWhenItThrows()
    {
        Assert.Throws<InvalidOperationException>(() => WorldPreviewCapture.Run(() => throw new InvalidOperationException()));

        Assert.False(WorldPreviewCapture.IsActive);
    }
}
