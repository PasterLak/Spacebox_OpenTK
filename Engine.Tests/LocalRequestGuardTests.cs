using Engine.Web;

namespace Engine.Tests;

public class LocalRequestGuardTests
{
    private static HttpRequest Request(string host, string origin = null)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Host"] = host };
        if (origin != null) headers["Origin"] = origin;
        return new HttpRequest { Headers = headers };
    }

    [Theory]
    [InlineData("127.0.0.1:47800")]
    [InlineData("localhost:47800")]
    [InlineData("[::1]:47800")]
    [InlineData("127.0.0.1")]
    public void LocalTools_AreAllowed(string host)
    {
        Assert.True(LocalRequestGuard.IsLocalTool(Request(host)));
    }

    [Fact]
    public void BrowserPages_AndRebindedHosts_AreRefused()
    {
        Assert.False(LocalRequestGuard.IsLocalTool(Request("127.0.0.1:47800", "https://evil.example")));
        Assert.False(LocalRequestGuard.IsLocalTool(Request("evil.example:47800")));
        Assert.False(LocalRequestGuard.IsLocalTool(Request("")));
    }
}
