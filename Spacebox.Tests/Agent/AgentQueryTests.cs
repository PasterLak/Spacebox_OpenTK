using System.Text;
using Engine.Web;
using OpenTK.Mathematics;
using Spacebox.Agent;

namespace Spacebox.Tests.Agent;

public class AgentQueryTests
{
    private static HttpRequest Request(Dictionary<string, string> query, string body = "")
    {
        return new HttpRequest { Query = query, Body = Encoding.UTF8.GetBytes(body) };
    }

    [Fact]
    public void Reads_NumbersWithInvariantCulture_AndFallbacks()
    {
        var request = Request(new Dictionary<string, string> { ["x"] = "1.5", ["y"] = "-2", ["z"] = "3e1", ["frames"] = "7.6", ["face"] = "yes" });

        Assert.Equal(new Vector3(1.5f, -2f, 30f), AgentQuery.Vector(request));
        Assert.Equal(8, AgentQuery.Int(request, "frames", 0));
        Assert.Equal(4f, AgentQuery.Float(request, "speed", 4f));
        Assert.True(AgentQuery.Bool(request, "face"));
        Assert.False(AgentQuery.Bool(request, "missing"));
    }

    [Fact]
    public void Throws_RequestErrors_ForMissingOrInvalidValues()
    {
        var request = Request(new Dictionary<string, string> { ["x"] = "abc" });

        Assert.Equal(400, Assert.Throws<AgentRequestException>(() => AgentQuery.Float(request, "x", 0)).Status);
        Assert.Throws<AgentRequestException>(() => AgentQuery.RequiredText(request, "text"));
        Assert.False(AgentQuery.HasVector(request));
    }

    [Fact]
    public void TextOrBody_PrefersQueryThenBody()
    {
        Assert.Equal("tp 0 0 0", AgentQuery.TextOrBody(Request(new Dictionary<string, string> { ["cmd"] = "tp 0 0 0" }, "help"), "cmd"));
        Assert.Equal("help", AgentQuery.TextOrBody(Request(new Dictionary<string, string>(), "help"), "cmd"));
        Assert.Throws<AgentRequestException>(() => AgentQuery.TextOrBody(Request(new Dictionary<string, string>()), "cmd"));
    }
}
