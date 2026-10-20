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
}
