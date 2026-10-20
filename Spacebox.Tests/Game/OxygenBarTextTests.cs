using Spacebox.GUI;

namespace Spacebox.Tests.Game;

public class OxygenBarTextTests
{
    [Theory]
    [InlineData(true, false, "Air")]
    [InlineData(true, true, "Air")]
    [InlineData(false, true, "Infinite")]
    [InlineData(false, false, null)]
    public void TextOverride_ShowsAirOrInfinite(bool isInAir, bool isInfinite, string expected)
    {
        Assert.Equal(expected, OxygenBar.GetTextOverride(isInAir, isInfinite));
    }
}
