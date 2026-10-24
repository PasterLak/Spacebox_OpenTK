using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Engine;
using Engine.Web;
using Spacebox.Network.Dashboard;
using Spacebox.Network.Dashboard.Modules;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Network;

public class DashboardTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spacebox_dashboard_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private DashboardAdminStore Store() => new DashboardAdminStore(Path.Combine(_root, "admin.json"), Path.Combine(_root, "setup-key.txt"));

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("abc")]
    [InlineData(null)]
    public void MapRadius_FallsBackToTheDefaultForBadValues(string text)
    {
        Assert.Equal(MapModule.DefaultRadius, MapModule.ParseRadius(text));
    }

    [Fact]
    public void MapRadius_KeepsFiniteValues()
    {
        Assert.Equal(1500f, MapModule.ParseRadius("1500"));
        Assert.Equal(-5f, MapModule.ParseRadius("-5"));
    }

    [Fact]
    public void SetupKey_IsCreatedOnceAndDeletedAfterRegistration()
    {
        var store = Store();
        string key = store.EnsureSetupKey();

        Assert.Matches("^[A-Z2-9]{5}(-[A-Z2-9]{5}){3}$", key);
        Assert.Equal(key, store.EnsureSetupKey());
        Assert.False(store.TryRegister("WRONG-KEY", "admin", "password1", out _));
        Assert.True(store.TryRegister(key.ToLowerInvariant(), "admin", "password1", out var error), error);
        Assert.False(File.Exists(Path.Combine(_root, "setup-key.txt")));
        Assert.Null(store.EnsureSetupKey());
        Assert.False(store.TryRegister(key, "other", "password1", out _));
    }

    [Fact]
    public void Admin_PasswordIsHashedAndVerified()
    {
        var store = Store();
        store.TryRegister(store.EnsureSetupKey(), "Admin", "correct horse", out _);

        Assert.DoesNotContain("correct horse", File.ReadAllText(Path.Combine(_root, "admin.json")));
        Assert.True(Store().Verify("admin", "correct horse"));
        Assert.False(Store().Verify("admin", "wrong"));
        Assert.False(Store().Verify("someone", "correct horse"));
    }

    [Fact]
    public void Admin_PasswordRulesAndChange()
    {
        var store = Store();
        Assert.False(store.TryRegister(store.EnsureSetupKey(), "admin", "short", out _));
        store.TryRegister(store.EnsureSetupKey(), "admin", "password1", out _);

        Assert.False(store.TryChangePassword("bad", "password2", out _));
        Assert.True(store.TryChangePassword("password1", "password2", out _));
        Assert.True(store.Verify("admin", "password2"));
        Assert.False(store.Verify("admin", "password1"));
    }

    [Fact]
    public void Sessions_ExpireAndSlide()
    {
        var now = new DateTime(2026, 1, 1);
        var sessions = new DashboardSessions(() => now);
        string token = sessions.Create("admin");

        now += DashboardSessions.Lifetime - TimeSpan.FromMinutes(1);
        Assert.True(sessions.TryGet(token, out var user));
        Assert.Equal("admin", user);

        now += DashboardSessions.Lifetime - TimeSpan.FromMinutes(1);
        Assert.True(sessions.TryGet(token, out _));

        now += DashboardSessions.Lifetime + TimeSpan.FromMinutes(1);
        Assert.False(sessions.TryGet(token, out _));
        Assert.False(sessions.TryGet("forged", out _));
    }

    [Fact]
    public void Sessions_EndAfterMaxLifetimeEvenWhenUsed()
    {
        var now = new DateTime(2026, 1, 1);
        var sessions = new DashboardSessions(() => now);
        string token = sessions.Create("admin");

        while (now + TimeSpan.FromHours(6) < new DateTime(2026, 1, 1) + DashboardSessions.MaxLifetime)
        {
            now += TimeSpan.FromHours(6);
            Assert.True(sessions.TryGet(token, out _));
        }

        now += TimeSpan.FromHours(6);
        Assert.False(sessions.TryGet(token, out _));
    }

    [Fact]
    public void Sessions_CookieIsHttpOnlyAndStrict()
    {
        string cookie = DashboardSessions.CookieHeader("abc");

        Assert.Contains("HttpOnly", cookie);
        Assert.Contains("SameSite=Strict", cookie);
        Assert.Contains("Path=/dashboard", cookie);
    }

    [Fact]
    public void Throttle_LocksAfterTooManyFailuresAndUnlocksLater()
    {
        var now = new DateTime(2026, 1, 1);
        var throttle = new LoginThrottle(() => now);

        for (int i = 0; i < LoginThrottle.MaxFailures; i++) throttle.RecordFailure("1.1.1.1");
        Assert.True(throttle.IsLocked("1.1.1.1"));
        Assert.False(throttle.IsLocked("2.2.2.2"));

        now += LoginThrottle.Window + TimeSpan.FromSeconds(1);
        Assert.False(throttle.IsLocked("1.1.1.1"));
    }

    [Fact]
    public void Throttle_CountsAttemptsBeforeTheyAreChecked()
    {
        var now = new DateTime(2026, 1, 1);
        var throttle = new LoginThrottle(() => now);

        for (int i = 0; i < LoginThrottle.MaxFailures; i++) Assert.True(throttle.TryBeginAttempt("1.1.1.1"));
        Assert.False(throttle.TryBeginAttempt("1.1.1.1"));

        throttle.Reset("1.1.1.1");
        Assert.True(throttle.TryBeginAttempt("1.1.1.1"));
    }

    [Fact]
    public void Throttle_GroupsIpv6ByPrefixAndLimitsAllAddresses()
    {
        var now = new DateTime(2026, 1, 1);
        var throttle = new LoginThrottle(() => now);

        for (int i = 0; i < LoginThrottle.MaxFailures; i++) throttle.RecordFailure($"2001:db8:1:2::{i + 1:x}");
        Assert.True(throttle.IsLocked("2001:db8:1:2:ffff::9"));
        Assert.False(throttle.IsLocked("2001:db8:1:3::1"));
        Assert.Equal("1.2.3.4", LoginThrottle.KeyOf("::ffff:1.2.3.4"));

        int allowed = 0;
        for (int i = 0; i < 100; i++)
            if (throttle.TryBeginAttempt($"85.0.{i / 250}.{i % 250}")) allowed++;
        Assert.Equal(LoginThrottle.MaxAttemptsPerMinute, allowed);

        now += TimeSpan.FromMinutes(2);
        Assert.True(throttle.TryBeginAttempt("85.9.9.9"));
        Assert.True(throttle.TryBeginAttempt("192.168.1.2"));
    }

    [Fact]
    public void Guard_RequiresSessionAndRequestHeaderForChanges()
    {
        var sessions = new DashboardSessions();
        var guard = new DashboardGuard(sessions);
        var handler = guard.Admin((_, user) => HttpResponse.Text(user));
        string token = sessions.Create("admin");
        var cookie = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Cookie"] = $"{DashboardSessions.CookieName}={token}" };
        var cookieAndHeader = new Dictionary<string, string>(cookie, StringComparer.OrdinalIgnoreCase) { [DashboardGuard.RequestHeader] = "1" };

        Assert.Equal(401, handler(new HttpRequest { Method = "GET" }).Result.Status);
        Assert.Equal(200, handler(new HttpRequest { Method = "GET", Headers = cookie }).Result.Status);
        Assert.Equal(403, handler(new HttpRequest { Method = "POST", Headers = cookie }).Result.Status);
        Assert.Equal(200, handler(new HttpRequest { Method = "POST", Headers = cookieAndHeader }).Result.Status);
    }

    [Fact]
    public void Log_KeepsOrderAndCapacity()
    {
        var log = new DashboardLog();
        for (int i = 0; i < DashboardLog.Capacity + 10; i++) log.Append("line " + i, Debug.MessageType.Info);

        var lines = log.After(log.LastId - 3);
        Assert.Equal(3, lines.Count);
        Assert.Equal("line " + (DashboardLog.Capacity + 9), lines[^1].Text);
        Assert.Equal(DashboardLog.MaxBatch, log.After(0).Count);

        log.Append("oops", Debug.MessageType.Error);
        Assert.Equal("error", log.After(log.LastId - 1)[0].Level);
    }

    [Fact]
    public void StaticFiles_BlockPathTraversal()
    {
        Directory.CreateDirectory(Path.Combine(_root, "web"));
        File.WriteAllText(Path.Combine(_root, "web", "index.html"), "ok");
        File.WriteAllText(Path.Combine(_root, "secret.json"), "no");

        Assert.True(StaticFilesModule.TryResolve(Path.Combine(_root, "web"), "index.html", out _));
        Assert.False(StaticFilesModule.TryResolve(Path.Combine(_root, "web"), "../secret.json", out _));
        Assert.False(StaticFilesModule.TryResolve(Path.Combine(_root, "web"), "..\\secret.json", out _));
        Assert.False(StaticFilesModule.TryResolve(Path.Combine(_root, "web"), "missing.html", out _));
    }

    [Fact]
    public void PlayerSave_ParsesStatsAndSlots()
    {
        const string json = "{\"Health\":80,\"Power\":50.5,\"Oxygen\":100,\"PositionX\":1,\"PositionY\":2,\"PositionZ\":3," +
                            "\"InventorySlots\":[{\"ItemID\":\"default:iron_ingot\",\"Count\":12,\"SlotX\":2,\"SlotY\":1},{\"ItemID\":\"\",\"Count\":0}]," +
                            "\"PanelSlots\":[],\"EquipmentSlots\":[{\"ItemID\":\"default:iron_helmet\",\"Count\":1,\"SlotX\":0,\"SlotY\":0}],\"Statistics\":{\"BlocksPlaced\":7}}";

        var view = PlayerSaveView.Parse(json);

        Assert.Equal(80f, view.Health);
        Assert.Equal(new[] { 1f, 2f, 3f }, view.Position);
        Assert.Single(view.Inventory);
        Assert.Equal(12, view.Inventory[0].Count);
        Assert.Equal("default:iron_helmet", view.Equipment[0].Id);
        Assert.Equal(7, view.Statistics["BlocksPlaced"].GetValue<int>());
        Assert.Null(PlayerSaveView.Parse("not json"));
    }

    [Fact]
    public void Settings_AreValidated()
    {
        var good = new ServerModule.SettingsRequest { Name = "My server", Description = "", MaxPlayers = 8, AutoSaveInterval = 300, UpdateRate = 30 };

        Assert.True(ServerModule.Validate(good, out _));
        Assert.False(ServerModule.Validate(new ServerModule.SettingsRequest { Name = "", MaxPlayers = 8, UpdateRate = 30 }, out _));
        Assert.False(ServerModule.Validate(new ServerModule.SettingsRequest { Name = "x", MaxPlayers = 0, UpdateRate = 30 }, out _));
        Assert.False(ServerModule.Validate(new ServerModule.SettingsRequest { Name = "x", MaxPlayers = 8, UpdateRate = 30, AutoSaveInterval = 3 }, out _));
        Assert.False(ServerModule.Validate(new ServerModule.SettingsRequest { Name = "x", MaxPlayers = 8, UpdateRate = 5 }, out _));
    }

    [Fact]
    public void Metrics_CpuPercentIsNormalizedByCores()
    {
        Assert.Equal(50.0, DashboardMetrics.CpuPercent(TimeSpan.FromSeconds(4), 2, 4), 3);
        Assert.Equal(100.0, DashboardMetrics.CpuPercent(TimeSpan.FromSeconds(100), 1, 1), 3);
        Assert.Equal(0.0, DashboardMetrics.CpuPercent(TimeSpan.FromSeconds(1), 0, 4), 3);
    }

    [Fact]
    public void ItemIcons_ReadTextureNamesFromTheDefaultGameSet()
    {
        string folder = Spacebox.Tests.Versioning.WorldToolkitTests.DefaultGameSet().DefaultGameSet;
        var names = DashboardItemIcons.ReadTextureNames(folder);

        Assert.True(names.ContainsKey("aluminium_drill"));
        Assert.True(names.ContainsKey("cobalt_power_block"));
    }

    private static X509Certificate2 SelfSigned(string name, out RSA key)
    {
        key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(name);
        request.CertificateExtensions.Add(names.Build());
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(60));
    }

    private (string Cert, string Key) WritePem(string name, string fileName = "fullchain.pem")
    {
        Directory.CreateDirectory(_root);
        using var certificate = SelfSigned(name, out var key);
        string certPath = Path.Combine(_root, fileName);
        string keyPath = Path.Combine(_root, "privkey.pem");
        File.WriteAllText(certPath, certificate.ExportCertificatePem());
        File.WriteAllText(keyPath, key.ExportPkcs8PrivateKeyPem());
        key.Dispose();
        return (certPath, keyPath);
    }

    [Fact]
    public void Certificate_LoadsPemWithPrivateKey()
    {
        var (cert, key) = WritePem("myserver.duckdns.org");
        var certificate = new DashboardCertificate(cert, key, "");

        Assert.True(certificate.IsConfigured);
        Assert.NotNull(certificate.Current);
        Assert.True(certificate.Current.HasPrivateKey);
        Assert.Contains("myserver.duckdns.org", certificate.DnsNames());
    }

    [Fact]
    public void Certificate_LoadsPfxWithPassword()
    {
        Directory.CreateDirectory(_root);
        using var created = SelfSigned("pfx.duckdns.org", out var key);
        key.Dispose();
        string path = Path.Combine(_root, "cert.pfx");
        File.WriteAllBytes(path, created.Export(X509ContentType.Pkcs12, "secret"));

        Assert.NotNull(new DashboardCertificate(path, "", "secret").Current);
        Assert.Null(new DashboardCertificate(path, "", "wrong").Current);
    }

    [Fact]
    public void Certificate_MissingOrBrokenFilesGiveNoCertificate()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "bad.pem"), "not a certificate");

        Assert.False(new DashboardCertificate("", "", "").IsConfigured);
        Assert.Null(new DashboardCertificate(Path.Combine(_root, "missing.pem"), "", "").Current);
        Assert.Null(new DashboardCertificate(Path.Combine(_root, "bad.pem"), Path.Combine(_root, "bad.pem"), "").Current);
        var (cert, _) = WritePem("nokey.example");
        var withoutKey = new DashboardCertificate(cert, Path.Combine(_root, "missing-key.pem"), "");
        Assert.Null(withoutKey.Current);
        Assert.Contains("private key", withoutKey.LastError);
    }

    [Fact]
    public void Certificate_ReloadsAfterRenewal()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var (cert, key) = WritePem("old.duckdns.org");
        var certificate = new DashboardCertificate(cert, key, "", () => now);
        string first = certificate.Current.Thumbprint;

        WritePem("new.duckdns.org");
        File.SetLastWriteTimeUtc(cert, DateTime.UtcNow.AddMinutes(5));
        Assert.Equal(first, certificate.Current.Thumbprint);

        now += DashboardCertificate.CheckInterval;
        Assert.NotEqual(first, certificate.Current.Thumbprint);
        Assert.Contains("new.duckdns.org", certificate.DnsNames());
    }

    private static HttpRequest Request(string address, bool secure, string method = "GET", string host = "me.duckdns.org:7777")
    {
        return new HttpRequest
        {
            Method = method,
            Path = "/dashboard/",
            RemoteAddress = address,
            IsSecure = secure,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Host"] = host }
        };
    }

    [Fact]
    public void AccessPolicy_RedirectsOrBlocksPublicPlainHttp()
    {
        var policy = new DashboardAccessPolicy("me.duckdns.org", allowPublicHttp: false);

        Assert.Null(policy.Check(Request("85.1.2.3", true), true));
        Assert.Null(policy.Check(Request("192.168.1.10", false), true));
        Assert.Null(policy.Check(Request("192.168.1.10", false), false));
        Assert.Equal(403, policy.Check(Request("85.1.2.3", false), false).Status);
        Assert.Null(new DashboardAccessPolicy("me.duckdns.org", allowPublicHttp: true).Check(Request("85.1.2.3", false), false));

        var redirect = policy.Check(Request("85.1.2.3", false), true);
        Assert.Equal(307, redirect.Status);
        Assert.Equal("https://me.duckdns.org:7777/dashboard/", redirect.Headers["Location"]);
        Assert.Equal(403, policy.Check(Request("85.1.2.3", false, "POST"), true).Status);
    }

    [Theory]
    [InlineData("192.168.1.5:7777", true)]
    [InlineData("[::1]:7777", true)]
    [InlineData("localhost", true)]
    [InlineData("myserver", true)]
    [InlineData("box.lan", true)]
    [InlineData("ME.duckdns.org:7777", true)]
    [InlineData("game.example.com", true)]
    [InlineData("a.example.com", false)]
    [InlineData("evil.attacker.com", false)]
    [InlineData("me.duckdns.org.attacker.com", false)]
    public void AccessPolicy_AllowsOnlyKnownHostNames(string host, bool allowed)
    {
        var policy = new DashboardAccessPolicy("me.duckdns.org", false, () => new[] { "game.example.com" });
        Assert.Equal(allowed, policy.IsAllowedHost(host));
    }

    [Fact]
    public void AccessPolicy_RejectsForeignHostWith421()
    {
        var request = Request("192.168.1.10", false, host: "rebind.attacker.com");
        Assert.Equal(421, new DashboardAccessPolicy("", false).Check(request, false).Status);
    }

    [Fact]
    public void Sessions_CookieIsSecureOnlyOverHttps()
    {
        Assert.Contains("; Secure", DashboardSessions.CookieHeader("t", true));
        Assert.DoesNotContain("Secure", DashboardSessions.CookieHeader("t", false));
    }

    [Theory]
    [InlineData("", "", "", "", HttpsMethod.Off)]
    [InlineData("myserver.duckdns.org", "tok", "", "", HttpsMethod.DuckDns)]
    [InlineData("myserver.duckdns.org", "", "", "", HttpsMethod.HttpPort)]
    [InlineData("play.example.com", "", "cf", "", HttpsMethod.Cloudflare)]
    [InlineData("play.example.com", "", "", "", HttpsMethod.HttpPort)]
    [InlineData("85.10.20.30", "tok", "cf", "", HttpsMethod.HttpPort)]
    [InlineData("play.example.com", "", "", "certs/fullchain.pem", HttpsMethod.CertificateFiles)]
    public void HttpsMethod_IsChosenFromTheConfig(string address, string duckToken, string cloudflare, string files, HttpsMethod expected)
    {
        var config = new ServerConfig { PublicAddress = address, DuckDnsToken = duckToken, CloudflareApiToken = cloudflare, DashboardCertificateFile = files };

        Assert.Equal(expected, HttpsMethodSelector.Select(config));
    }

    [Fact]
    public void HttpsMethod_CanBeTurnedOffAndAcceptsTheOldDuckDnsField()
    {
        Assert.Equal(HttpsMethod.Off, HttpsMethodSelector.Select(new ServerConfig { PublicAddress = "play.example.com", HttpsAutomatic = false }));

        var old = new ServerConfig { DuckDnsDomain = "Me", DuckDnsToken = "t" };
        Assert.Equal("me.duckdns.org", HttpsMethodSelector.PublicAddress(old));
        Assert.Equal(HttpsMethod.DuckDns, HttpsMethodSelector.Select(old));
        Assert.True(HttpsMethodSelector.UpdatesDuckDnsIp(old));
        Assert.False(HttpsMethodSelector.UpdatesDuckDnsIp(new ServerConfig { PublicAddress = "play.example.com", DuckDnsToken = "t" }));
    }

    [Theory]
    [InlineData("https://Play.Example.com:7777/dashboard", "play.example.com")]
    [InlineData("play.example.com.", "play.example.com")]
    [InlineData(" 85.10.20.30 ", "85.10.20.30")]
    [InlineData("85.10.20.30:7777", "85.10.20.30")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    public void HttpsMethod_NormalizesTheAddress(string input, string expected)
    {
        Assert.Equal(expected, HttpsMethodSelector.Normalize(input));
    }

    [Fact]
    public void AutoCertificate_StoresFilesPerAddress()
    {
        var service = new AutoCertificateService(new ServerConfig { PublicAddress = "Play.Example.com" }, _root);

        Assert.True(AutoCertificateService.IsConfigured(new ServerConfig { PublicAddress = "play.example.com" }));
        Assert.False(AutoCertificateService.IsConfigured(new ServerConfig()));
        Assert.Equal("play.example.com", service.Domain);
        Assert.Equal(HttpsMethod.HttpPort, service.Method);
        Assert.Equal(Path.Combine(_root, "play.example.com", "fullchain.pem"), service.CertificateFile);
    }

    [Fact]
    public void AutoCertificate_RenewsMissingAndSoonExpiringCertificates()
    {
        var (cert, _) = WritePem("me.duckdns.org");

        Assert.True(AutoCertificateService.NeedsRenewal(Path.Combine(_root, "missing.pem"), DateTime.UtcNow, out _));
        Assert.False(AutoCertificateService.NeedsRenewal(cert, DateTime.UtcNow, out var expires));
        Assert.True(expires > DateTime.UtcNow.AddDays(50));
        Assert.True(AutoCertificateService.NeedsRenewal(cert, DateTime.UtcNow.AddDays(40), out _));
    }

    [Fact]
    public void AutoCertificate_RenewsShortLivedCertificatesAfterTwoThirds()
    {
        Directory.CreateDirectory(_root);
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=85.10.20.30", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var start = DateTimeOffset.UtcNow;
        using (var shortLived = request.CreateSelfSigned(start, start.AddDays(6)))
            File.WriteAllText(Path.Combine(_root, "short.pem"), shortLived.ExportCertificatePem());

        Assert.False(AutoCertificateService.NeedsRenewal(Path.Combine(_root, "short.pem"), DateTime.UtcNow.AddDays(3), out _));
        Assert.True(AutoCertificateService.NeedsRenewal(Path.Combine(_root, "short.pem"), DateTime.UtcNow.AddDays(4.5), out _));
    }

    [Fact]
    public void Config_DashboardPortFallsBackToTheGamePort()
    {
        Assert.Equal(7777, new ServerConfig { Port = 7777 }.GetDashboardPort());
        Assert.Equal(8080, new ServerConfig { Port = 7777, DashboardPort = 8080 }.GetDashboardPort());
        Assert.True(new ServerConfig().DashboardEnabled);
    }
}
