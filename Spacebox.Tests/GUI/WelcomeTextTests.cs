using Spacebox.Game.GUI;

namespace Spacebox.Tests.GUI;

public class WelcomeTextTests
{
    [Fact]
    public void WelcomeTextNamesTheGivenControlsKey()
    {
        string text = WelcomeUI.BuildText("F2");

        Assert.Contains("Press F2 to see the controls.", text);
        Assert.DoesNotContain("F6", text);
    }
}
