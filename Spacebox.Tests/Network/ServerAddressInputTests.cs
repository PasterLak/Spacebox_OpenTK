using Spacebox.Game.GUI.Menu;
using Spacebox.Network.Discovery;
using Xunit;

namespace Spacebox.Tests.Network;

public class ServerAddressInputTests
{
    [Theory]
    [InlineData("192.168.1.5:7777", "192.168.1.5", 7777)]
    [InlineData(" play.example.com:25000 ", "play.example.com", 25000)]
    [InlineData("[::1]:9000", "::1", 9000)]
    public void TrySplitHostPort_SplitsAddress(string text, string host, int port)
    {
        Assert.True(ServerAddressValidator.TrySplitHostPort(text, out var parsedHost, out var parsedPort));
        Assert.Equal(host, parsedHost);
        Assert.Equal(port, parsedPort);
    }

    [Theory]
    [InlineData("192.168.1.5")]
    [InlineData("192.168.1.5:")]
    [InlineData("192.168.1.5:abc")]
    [InlineData("192.168.1.5:70000")]
    [InlineData("::1")]
    [InlineData("")]
    public void TrySplitHostPort_RejectsWithoutValidPort(string text)
    {
        Assert.False(ServerAddressValidator.TrySplitHostPort(text, out _, out _));
    }

    [Fact]
    public void ClipboardText_TakesFirstLineAndLimitsLength()
    {
        Assert.Equal("10.0.0.1:7777", ClipboardText.Clean("  10.0.0.1:7777\r\nsecond line", 64));
        Assert.Equal("abc", ClipboardText.Clean("a\tbcdef", 3));
        Assert.Equal("", ClipboardText.Clean(null, 10));
    }
}
