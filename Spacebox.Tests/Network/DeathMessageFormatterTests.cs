using Spacebox.Network.Players;
using Xunit;

namespace Spacebox.Tests.Network;

public class DeathMessageFormatterTests
{
    [Fact]
    public void Format_PrefersKiller()
    {
        Assert.Equal("Bob was killed by Alice", DeathMessageFormatter.Format("Bob", "Alice", "Killed by Alice"));
    }

    [Fact]
    public void Format_UsesReason()
    {
        Assert.Equal("Bob died: Ran out of oxygen", DeathMessageFormatter.Format("Bob", null, "Ran out of oxygen"));
    }

    [Fact]
    public void Format_WithoutReason()
    {
        Assert.Equal("Bob died", DeathMessageFormatter.Format("Bob", null, "  "));
        Assert.Equal("Bob died", DeathMessageFormatter.Format("Bob", "", null));
    }

    [Fact]
    public void Sanitize_RemovesControlCharsAndLimitsLength()
    {
        Assert.Equal("a b", DeathMessageFormatter.Sanitize("a\nb\r"));
        Assert.Equal(DeathMessageFormatter.MaxReasonLength, DeathMessageFormatter.Sanitize(new string('x', 500)).Length);
    }
}
