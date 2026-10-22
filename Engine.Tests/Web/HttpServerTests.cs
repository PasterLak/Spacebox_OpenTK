using System.Net.Http;
using System.Text;
using Engine.Web;

namespace Engine.Tests.Web;

public class HttpServerTests
{
    private static Task<HttpRequest> Parse(string raw, int maxBody = 1024)
    {
        return HttpProtocol.ReadRequestAsync(new MemoryStream(Encoding.ASCII.GetBytes(raw)), "1.2.3.4", maxBody, CancellationToken.None);
    }

    [Fact]
    public async Task Parse_ReadsMethodPathQueryHeadersAndBody()
    {
        var request = await Parse("POST /dashboard/api/x?after=5&name=A%20B HTTP/1.1\r\nHost: s\r\nCookie: a=1; sbx=tok\r\nContent-Length: 4\r\n\r\nbody");

        Assert.Equal("POST", request.Method);
        Assert.Equal("/dashboard/api/x", request.Path);
        Assert.Equal("5", request.QueryValue("after"));
        Assert.Equal("A B", request.QueryValue("name"));
        Assert.Equal("tok", request.Cookie("sbx"));
        Assert.Equal("body", request.BodyText);
        Assert.Equal("1.2.3.4", request.RemoteAddress);
    }

    [Fact]
    public async Task Parse_RejectsTooLargeBody()
    {
        await Assert.ThrowsAsync<PayloadTooLargeException>(() => Parse("POST / HTTP/1.1\r\nContent-Length: 5000\r\n\r\n", 100));
    }

    [Fact]
    public async Task Parse_RejectsBadRequestLine()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Parse("GARBAGE\r\n\r\n"));
    }

    [Theory]
    [InlineData("POST /x HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n")]
    [InlineData("POST /x HTTP/1.1\r\nContent-Length: 1\r\nContent-Length: 2\r\n\r\nab")]
    [InlineData("GET /x HTTP/1.1\r\nHost: a\r\nHost: b\r\n\r\n")]
    [InlineData("POST /x HTTP/1.1\r\nContent-Length: +1\r\n\r\na")]
    [InlineData("POST /x HTTP/1.1\r\nContent-Length : 1\r\n\r\na")]
    [InlineData("GET /x HTTP/2.0\r\n\r\n")]
    [InlineData("GET /x HTTP/1.1\r\nContent-Length: 3\r\n\r\nabc")]
    public async Task Parse_RejectsAmbiguousRequests(string raw)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Parse(raw));
    }

    [Fact]
    public void ConnectionCounter_LimitsEachAddressButNotLoopback()
    {
        var counter = new ConnectionCounter(2);
        Assert.True(counter.TryEnter("203.0.113.5"));
        Assert.True(counter.TryEnter("203.0.113.5"));
        Assert.False(counter.TryEnter("203.0.113.5"));
        Assert.True(counter.TryEnter("203.0.113.6"));
        counter.Leave("203.0.113.5");
        Assert.True(counter.TryEnter("203.0.113.5"));
        for (int i = 0; i < 10; i++) Assert.True(counter.TryEnter("127.0.0.1"));
    }

    [Fact]
    public async Task WriteResponse_StripsLineBreaksFromHeaders()
    {
        var request = await Parse("GET /dashboard/%0d%0aSet-Cookie:%20sbx=evil HTTP/1.1\r\nHost: s\r\n\r\n");
        var response = HttpResponse.Redirect("https://s" + request.Path);
        response.Headers["X-Test\r\nInjected"] = "a\nb";

        var stream = new MemoryStream();
        await HttpProtocol.WriteResponseAsync(stream, response, CancellationToken.None);
        string text = Encoding.ASCII.GetString(stream.ToArray());

        Assert.DoesNotContain("\r\nSet-Cookie", text);
        Assert.DoesNotContain("\r\nInjected", text);
        Assert.Contains("Location: https://s/dashboard/Set-Cookie: sbx=evil\r\n", text);
        Assert.Contains("X-TestInjected: ab\r\n", text);
    }

    [Fact]
    public async Task Router_MatchesExactThenPrefixAndReportsWrongMethod()
    {
        var router = new HttpRouter();
        router.Map("GET", "/a", _ => HttpResponse.Text("exact"));
        router.MapPrefix("GET", "/files/", (_, rest) => HttpResponse.Text("file " + rest));

        Assert.Equal("exact", Encoding.UTF8.GetString((await router.Handle(new HttpRequest { Method = "GET", Path = "/a" })).Body));
        Assert.Equal("file x/y.js", Encoding.UTF8.GetString((await router.Handle(new HttpRequest { Method = "GET", Path = "/files/x/y.js" })).Body));
        Assert.Equal(405, (await router.Handle(new HttpRequest { Method = "POST", Path = "/a" })).Status);
        Assert.Equal(404, (await router.Handle(new HttpRequest { Method = "GET", Path = "/missing" })).Status);
    }

    [Fact]
    public async Task Server_AnswersRealRequestsOnLoopback()
    {
        using var server = new MiniHttpServer(request => Task.FromResult(HttpResponse.Json(new { path = request.Path, body = request.BodyText })));
        server.Start(0);

        using var client = new HttpClient();
        var response = await client.PostAsync($"http://127.0.0.1:{server.Port}/hello", new StringContent("ping"));
        string text = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("\"path\":\"/hello\"", text);
        Assert.Contains("\"body\":\"ping\"", text);
    }

    [Fact]
    public async Task Server_BoundToLoopback_AnswersOnLoopback()
    {
        using var server = new MiniHttpServer(request => Task.FromResult(HttpResponse.Text(request.Path)));
        server.Start(0, System.Net.IPAddress.Loopback);

        using var client = new HttpClient();
        string text = await client.GetStringAsync($"http://127.0.0.1:{server.Port}/agent");

        Assert.Equal("/agent", text);
        Assert.True(server.IsRunning);
    }
}
