using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class ShipViewCameraTests
{
    [Theory]
    [InlineData(90f, 108f)]
    [InlineData(50f, 60f)]
    [InlineData(120f, 144f)]
    public void GetViewFov_IsWiderThanPlayerFov(float playerFov, float expected)
    {
        Assert.Equal(expected, ShipViewCamera.GetViewFov(playerFov), 3);
    }

    [Fact]
    public void GetViewFov_IsCapped()
    {
        Assert.Equal(ShipViewCamera.MaxFov, ShipViewCamera.GetViewFov(170f));
    }
}
