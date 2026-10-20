using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Engine.Web;

namespace Engine.Tests.Web;

public class HttpsTests
{
    public static X509Certificate2 SelfSigned(string name = "localhost")
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(name);
        request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return new X509Certificate2(certificate.Export(X509ContentType.Pkcs12), (string)null, X509KeyStorageFlags.Exportable);
    }

    [Fact]
    public void PrefixedStream_ReturnsPrefixThenInner()
    {
        using var stream = new PrefixedStream(new MemoryStream(Encoding.ASCII.GetBytes("BC")), new[] { (byte)'A' });
        var buffer = new byte[8];
        int total = 0, read;
        while ((read = stream.Read(buffer, total, buffer.Length - total)) > 0) total += read;

        Assert.Equal("ABC", Encoding.ASCII.GetString(buffer, 0, total));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.16.0.5", true)]
    [InlineData("172.31.255.1", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.1.20", true)]
    [InlineData("169.254.3.3", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("::1", true)]
    [InlineData("fd12:3456::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("2001:4860:4860::8888", false)]
    [InlineData("::ffff:192.168.0.2", true)]
    [InlineData("::ffff:1.1.1.1", false)]
    [InlineData("not an ip", false)]
    public void NetworkAddresses_DetectLocalNetworks(string address, bool local)
    {
        Assert.Equal(local, NetworkAddresses.IsLocal(address));
    }

    [Fact]
    public async Task Server_AnswersHttpsAndHttpOnTheSamePort()
    {
        using var certificate = SelfSigned();
        using var server = new MiniHttpServer(request => Task.FromResult(HttpResponse.Text(request.IsSecure ? "secure" : "plain")))
        {
            CertificateProvider = () => certificate
        };
        server.Start(0);

        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert?.Thumbprint == certificate.Thumbprint };
        using var client = new HttpClient(handler);

        Assert.Equal("secure", await client.GetStringAsync($"https://localhost:{server.Port}/"));
        Assert.Equal("plain", await client.GetStringAsync($"http://127.0.0.1:{server.Port}/"));
    }

    [Fact]
    public async Task Server_WithoutCertificate_DropsTlsAndKeepsServingHttp()
    {
        using var server = new MiniHttpServer(_ => Task.FromResult(HttpResponse.Text("plain")));
        server.Start(0);

        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var client = new HttpClient(handler);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetStringAsync($"https://localhost:{server.Port}/"));
        Assert.Equal("plain", await client.GetStringAsync($"http://127.0.0.1:{server.Port}/"));
    }
}
