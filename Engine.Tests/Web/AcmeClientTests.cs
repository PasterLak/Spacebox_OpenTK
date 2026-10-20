using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using Engine.Web;
using Engine.Web.Acme;

namespace Engine.Tests.Web;

public class AcmeClientTests
{
    private sealed class FakeDns : IDnsChallenge
    {
        public string Txt;
        public bool Cleared;

        public Task SetTxtAsync(string domain, string value, CancellationToken token)
        {
            Txt = value;
            return Task.CompletedTask;
        }

        public Task ClearTxtAsync(string domain, CancellationToken token)
        {
            Cleared = true;
            Txt = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAcme
    {
        private readonly FakeDns _dns;
        private readonly HashSet<string> _nonces = new HashSet<string>();
        private readonly X509Certificate2 _ca;
        private JsonObject _accountJwk;
        private string _authorizationStatus = "pending";
        private string _orderStatus = "pending";
        private string _certificatePem;
        private int _counter;

        public string Base;
        public bool BreakDns;
        public int BadSignatures;
        public HttpChallengeSolver HttpSolver;
        public JsonObject LastOrder;

        public FakeAcme(FakeDns dns)
        {
            _dns = dns;
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=Fake CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            _ca = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        }

        private string NewNonce()
        {
            string nonce = "n" + Interlocked.Increment(ref _counter);
            lock (_nonces) _nonces.Add(nonce);
            return nonce;
        }

        private HttpResponse Reply(object body, int status = 200, string location = null)
        {
            var response = body is string pem ? HttpResponse.Bytes(Encoding.UTF8.GetBytes(pem), "application/pem-certificate-chain", status) : HttpResponse.Json(body, status);
            response.Headers["Replay-Nonce"] = NewNonce();
            if (location != null) response.Headers["Location"] = location;
            return response;
        }

        private bool Check(HttpRequest request, out JsonObject payload)
        {
            payload = null;
            var body = JsonNode.Parse(request.BodyText).AsObject();
            var header = JsonNode.Parse(Encoding.UTF8.GetString(AcmeJws.FromBase64Url(body["protected"].GetValue<string>()))).AsObject();

            string nonce = header["nonce"].GetValue<string>();
            lock (_nonces)
            {
                if (!_nonces.Remove(nonce)) return false;
            }
            if (header["url"].GetValue<string>() != Base + request.Path) return false;

            var jwk = header["jwk"]?.AsObject() ?? _accountJwk;
            if (jwk == null || (header["kid"] != null && header["kid"].GetValue<string>() != Base + "/account/1")) return false;
            _accountJwk ??= JsonNode.Parse(jwk.ToJsonString()).AsObject();

            string signed = body["protected"].GetValue<string>() + "." + body["payload"].GetValue<string>();
            using var key = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = AcmeJws.FromBase64Url(jwk["x"].GetValue<string>()), Y = AcmeJws.FromBase64Url(jwk["y"].GetValue<string>()) }
            });
            if (!key.VerifyData(Encoding.ASCII.GetBytes(signed), AcmeJws.FromBase64Url(body["signature"].GetValue<string>()), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                BadSignatures++;
                return false;
            }

            string payloadPart = body["payload"].GetValue<string>();
            payload = payloadPart.Length == 0 ? null : JsonNode.Parse(Encoding.UTF8.GetString(AcmeJws.FromBase64Url(payloadPart))).AsObject();
            return true;
        }

        private string Thumbprint()
        {
            string canonical = $"{{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"{_accountJwk["x"]}\",\"y\":\"{_accountJwk["y"]}\"}}";
            return AcmeJws.Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        }

        public Task<HttpResponse> Handle(HttpRequest request)
        {
            if (request.Path == "/directory")
                return Task.FromResult(HttpResponse.Json(new { newNonce = Base + "/nonce", newAccount = Base + "/account", newOrder = Base + "/order" }));
            if (request.Path == "/nonce") return Task.FromResult(Reply(new { }));
            if (!Check(request, out var payload)) return Task.FromResult(Reply(new { type = "urn:ietf:params:acme:error:malformed", detail = "bad request" }, 400));

            switch (request.Path)
            {
                case "/account":
                    return Task.FromResult(Reply(new { status = "valid" }, 201, Base + "/account/1"));
                case "/order":
                    LastOrder = payload;
                    return Task.FromResult(Reply(new { status = _orderStatus, authorizations = new[] { Base + "/authz/1" }, finalize = Base + "/finalize" }, 201, Base + "/order/1"));
                case "/authz/1":
                    return Task.FromResult(Reply(new
                    {
                        status = _authorizationStatus,
                        challenges = new object[] { new { type = "http-01", url = Base + "/chall/http", token = "tok123" }, new { type = "dns-01", url = Base + "/chall/1", token = "tok123" } }
                    }));
                case "/chall/1":
                    string expected = AcmeJws.Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes("tok123." + Thumbprint())));
                    _authorizationStatus = !BreakDns && _dns.Txt == expected ? "valid" : "invalid";
                    return Task.FromResult(Reply(new { status = "processing" }));
                case "/chall/http":
                    string answer;
                    using (var fetch = new HttpClient())
                    {
                        try
                        {
                            answer = fetch.GetStringAsync($"http://127.0.0.1:{HttpSolver.Port}{HttpChallengeSolver.PathPrefix}tok123").Result;
                        }
                        catch (Exception)
                        {
                            answer = null;
                        }
                    }
                    _authorizationStatus = answer == "tok123." + Thumbprint() ? "valid" : "invalid";
                    return Task.FromResult(Reply(new { status = "processing" }));
                case "/finalize":
                    var csr = CertificateRequest.LoadSigningRequest(AcmeJws.FromBase64Url(payload["csr"].GetValue<string>()), HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
                    using (var issued = csr.Create(_ca.SubjectName, X509SignatureGenerator.CreateForRSA(_ca.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1), DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(90), new byte[] { 1, 2, 3, 4 }))
                        _certificatePem = issued.ExportCertificatePem() + "\n" + _ca.ExportCertificatePem();
                    _orderStatus = "valid";
                    return Task.FromResult(Reply(new { status = "processing" }));
                case "/order/1":
                    return Task.FromResult(Reply(new { status = _orderStatus, certificate = Base + "/cert/1" }));
                case "/cert/1":
                    return Task.FromResult(Reply(_certificatePem));
            }

            return Task.FromResult(HttpResponse.NotFound());
        }
    }

    private static (MiniHttpServer Server, FakeAcme Acme, FakeDns Dns) StartFake()
    {
        var dns = new FakeDns();
        var acme = new FakeAcme(dns);
        var server = new MiniHttpServer(acme.Handle);
        server.Start(0);
        acme.Base = $"http://127.0.0.1:{server.Port}";
        return (server, acme, dns);
    }

    private static AcmeClient Client(FakeAcme acme, ECDsa key, HttpClient http)
    {
        return new AcmeClient(http, acme.Base + "/directory", key, null, TimeSpan.FromMilliseconds(10));
    }

    private static DnsChallengeSolver Dns(FakeDns dns) => new DnsChallengeSolver(dns, TimeSpan.Zero);

    [Fact]
    public async Task Issue_RunsTheWholeFlowAndReturnsAUsableCertificate()
    {
        var (server, acme, dns) = StartFake();
        using (server)
        using (var key = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        using (var http = new HttpClient())
        {
            var result = await Client(acme, key, http).IssueAsync("me.duckdns.org", "a@b.c", Dns(dns), CancellationToken.None);

            using var certificate = X509Certificate2.CreateFromPem(result.CertificatePem, result.PrivateKeyPem);
            Assert.True(certificate.HasPrivateKey);
            Assert.Equal("me.duckdns.org", certificate.GetNameInfo(X509NameType.SimpleName, false));
            Assert.True(dns.Cleared);
            Assert.Equal(0, acme.BadSignatures);
        }
    }

    [Fact]
    public async Task Issue_FailsClearlyWhenTheDnsRecordIsWrong()
    {
        var (server, acme, dns) = StartFake();
        acme.BreakDns = true;
        using (server)
        using (var key = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        using (var http = new HttpClient())
        {
            var error = await Assert.ThrowsAsync<AcmeException>(() => Client(acme, key, http).IssueAsync("me.duckdns.org", "", Dns(dns), CancellationToken.None));

            Assert.Contains("invalid", error.Message);
            Assert.True(dns.Cleared);
        }
    }

    [Fact]
    public async Task Issue_WithHttpCheckOnAPortWorksForAnyNameAndStopsThePort()
    {
        var (server, acme, _) = StartFake();
        var solver = new HttpChallengeSolver(0);
        acme.HttpSolver = solver;
        using (server)
        using (var key = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        using (var http = new HttpClient())
        {
            var result = await Client(acme, key, http).IssueAsync("play.example.com", "", solver, CancellationToken.None);

            using var certificate = X509Certificate2.CreateFromPem(result.CertificatePem, result.PrivateKeyPem);
            Assert.Equal("play.example.com", certificate.GetNameInfo(X509NameType.SimpleName, false));
            Assert.Equal("dns", acme.LastOrder["identifiers"][0]["type"].GetValue<string>());
            Assert.Null(acme.LastOrder["profile"]);
        }
    }

    [Fact]
    public async Task Issue_ForAnIpAddressUsesIpIdentifierAndProfile()
    {
        var (server, acme, _) = StartFake();
        var solver = new HttpChallengeSolver(0);
        acme.HttpSolver = solver;
        using (server)
        using (var key = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        using (var http = new HttpClient())
        {
            var result = await Client(acme, key, http).IssueAsync("203.0.113.7", "", solver, CancellationToken.None, AcmeClient.ShortLivedProfile);

            using var certificate = X509Certificate2.CreateFromPem(result.CertificatePem, result.PrivateKeyPem);
            Assert.Equal("ip", acme.LastOrder["identifiers"][0]["type"].GetValue<string>());
            Assert.Equal("shortlived", acme.LastOrder["profile"].GetValue<string>());
            Assert.Contains("203.0.113.7", certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Single().EnumerateIPAddresses().Select(a => a.ToString()));
        }
    }

    [Fact]
    public async Task HttpSolver_AnswersOnlyKnownTokens()
    {
        var solver = new HttpChallengeSolver(0);
        await solver.PrepareAsync("x", "abc", "abc.thumb", "", CancellationToken.None);
        using var http = new HttpClient();

        Assert.Equal("abc.thumb", await http.GetStringAsync($"http://127.0.0.1:{solver.Port}{HttpChallengeSolver.PathPrefix}abc"));
        var missing = await http.GetAsync($"http://127.0.0.1:{solver.Port}{HttpChallengeSolver.PathPrefix}other");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, missing.StatusCode);

        int port = solver.Port;
        await solver.CleanupAsync("x", CancellationToken.None);
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => http.GetStringAsync($"http://127.0.0.1:{port}{HttpChallengeSolver.PathPrefix}abc"));
    }

    [Fact]
    public void Cloudflare_ZoneCandidatesGoFromFullNameToRoot()
    {
        Assert.Equal(new[] { "a.play.example.co.uk", "play.example.co.uk", "example.co.uk", "co.uk" }, CloudflareDnsClient.ZoneCandidates("a.play.example.co.uk").ToArray());
        Assert.Equal(new[] { "example.com" }, CloudflareDnsClient.ZoneCandidates("example.com").ToArray());
    }

    [Fact]
    public async Task Cloudflare_CreatesAndDeletesTheTxtRecord()
    {
        var calls = new List<string>();
        string auth = null;
        using var server = new MiniHttpServer(request =>
        {
            auth = request.Header("Authorization");
            calls.Add(request.Method + " " + request.Path + (request.QueryValue("name") is { } name ? "?" + name : "") + (request.Body.Length > 0 ? " " + request.BodyText : ""));
            if (request.Method == "GET")
                return Task.FromResult(HttpResponse.Text(request.QueryValue("name") == "example.com" ? "{\"success\":true,\"result\":[{\"id\":\"zone1\"}]}" : "{\"success\":true,\"result\":[]}"));
            if (request.Method == "POST") return Task.FromResult(HttpResponse.Text("{\"success\":true,\"result\":{\"id\":\"rec9\"}}"));
            return Task.FromResult(HttpResponse.Text("{\"success\":true}"));
        });
        server.Start(0);
        using var http = new HttpClient();
        var client = new CloudflareDnsClient(http, "cf-token", $"http://127.0.0.1:{server.Port}");

        await client.SetTxtAsync("play.example.com", "val", CancellationToken.None);
        await client.ClearTxtAsync("play.example.com", CancellationToken.None);

        Assert.Equal("Bearer cf-token", auth);
        Assert.Equal("GET /zones?play.example.com", calls[0]);
        Assert.Equal("GET /zones?example.com", calls[1]);
        Assert.Contains("_acme-challenge.play.example.com", calls[2]);
        Assert.StartsWith("POST /zones/zone1/dns_records", calls[2]);
        Assert.Equal("DELETE /zones/zone1/dns_records/rec9", calls[3]);
    }

    [Fact]
    public async Task Cloudflare_ReportsMissingZone()
    {
        using var server = new MiniHttpServer(_ => Task.FromResult(HttpResponse.Text("{\"success\":true,\"result\":[]}")));
        server.Start(0);
        using var http = new HttpClient();

        var error = await Assert.ThrowsAsync<AcmeException>(() => new CloudflareDnsClient(http, "t", $"http://127.0.0.1:{server.Port}").SetTxtAsync("x.example.com", "v", CancellationToken.None));
        Assert.Contains("no zone", error.Message);
    }

    [Fact]
    public void Jws_TxtValueIsHashOfKeyAuthorization()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jws = new AcmeJws(key);

        string expected = AcmeJws.Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes("abc." + jws.Thumbprint())));
        Assert.Equal(expected, jws.DnsTxtValue("abc"));
        Assert.Equal(43, jws.Thumbprint().Length);
        Assert.DoesNotContain("=", jws.Thumbprint());
    }

    [Fact]
    public void Jws_RejectsNonP256Keys()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Assert.Throws<ArgumentException>(() => new AcmeJws(key));
    }

    [Theory]
    [InlineData("myserver", "myserver")]
    [InlineData("MyServer.duckdns.org", "myserver")]
    [InlineData("www.myserver.duckdns.org", "myserver")]
    public void DuckDns_NormalizesDomains(string input, string sub)
    {
        Assert.Equal(sub, DuckDnsClient.SubDomain(input));
        Assert.Equal(sub + ".duckdns.org", DuckDnsClient.FullName(input));
    }

    [Fact]
    public async Task DuckDns_SendsDomainTokenAndTxt()
    {
        string seen = null;
        using var server = new MiniHttpServer(request =>
        {
            seen = request.Path + "?" + string.Join("&", request.Query.Select(p => p.Key + "=" + p.Value));
            return Task.FromResult(HttpResponse.Text("OK"));
        });
        server.Start(0);
        using var http = new HttpClient();
        var client = new DuckDnsClient(http, "tok-1", $"http://127.0.0.1:{server.Port}/update");

        await client.SetTxtAsync("me.duckdns.org", "abc", CancellationToken.None);
        Assert.Equal("/update?domains=me&token=tok-1&txt=abc", seen);

        await client.UpdateIpAsync("me", CancellationToken.None);
        Assert.Equal("/update?domains=me&token=tok-1&ip=", seen);
    }

    [Fact]
    public async Task DuckDns_ThrowsWhenRefused()
    {
        using var server = new MiniHttpServer(_ => Task.FromResult(HttpResponse.Text("KO")));
        server.Start(0);
        using var http = new HttpClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new DuckDnsClient(http, "bad", $"http://127.0.0.1:{server.Port}/update").UpdateIpAsync("me", CancellationToken.None));
    }
}
