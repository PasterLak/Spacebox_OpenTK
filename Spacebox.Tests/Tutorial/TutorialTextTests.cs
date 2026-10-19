using Spacebox.Game.Tutorial;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class TutorialTextTests
{
    private sealed class Resolver : ITutorialTextResolver
    {
        public string KeyName(string action) => action == "forward" ? "W" : null;
        public string ItemName(string itemKey) => itemKey == "furnace" ? "Furnace" : null;
    }

    [Fact]
    public void Format_ReplacesKnownTokens()
    {
        var text = TutorialText.Format("Press {key:forward}, place the {block:furnace}, fly {target} blocks", new Resolver(), 20f);

        Assert.Equal("Press W, place the Furnace, fly 20 blocks", text);
    }

    [Fact]
    public void Format_KeepsUnknownTokensAndBrokenBraces()
    {
        Assert.Equal("Use {key:jump} and {item:x} {oops", TutorialText.Format("Use {key:jump} and {item:x} {oops", new Resolver()));
        Assert.Equal("", TutorialText.Format(null, new Resolver()));
        Assert.Equal("1.5", TutorialText.FormatNumber(1.5f));
    }
}
