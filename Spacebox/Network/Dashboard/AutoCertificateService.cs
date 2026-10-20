using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Engine;
using Engine.Web.Acme;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network.Dashboard;

public sealed class AutoCertificateService : IDisposable
{
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);
    public static readonly TimeSpan RetryInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan IpUpdateInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DuckDnsWait = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan CloudflareWait = TimeSpan.FromSeconds(15);

    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    private readonly ServerConfig _config;
    private readonly HttpsMethod _method;
    private readonly string _address;
    private readonly string _folder;
    private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
    private string _lastIpError = "";
    private bool _ipConfirmed;

    public AutoCertificateService(ServerConfig config, string certificatesFolder)
    {
        _config = config;
        _method = HttpsMethodSelector.Select(config);
        _address = HttpsMethodSelector.PublicAddress(config);
        _folder = Path.Combine(certificatesFolder, _address.Replace(':', '_') + (config.LetsEncryptStaging ? "-staging" : ""));
    }

    public static bool IsConfigured(ServerConfig config)
    {
        var method = HttpsMethodSelector.Select(config);
        return method != HttpsMethod.Off && method != HttpsMethod.CertificateFiles;
    }

    public string Domain => _address;
    public HttpsMethod Method => _method;
    public string CertificateFile => Path.Combine(_folder, "fullchain.pem");
    public string KeyFile => Path.Combine(_folder, "privkey.pem");
    public string Status { get; private set; } = "starting";
    public HttpChallengeSolver SharedHttpSolver { get; set; }

    public event Action CertificateChanged;

    public void Start()
    {
        if (HttpsMethodSelector.UpdatesDuckDnsIp(_config)) _ = RunIpUpdates(_cancel.Token);
        if (!IsConfigured(_config)) return;

        if (Engine.Web.NetworkAddresses.IsLocal(_address) || _address == "localhost")
        {
            Status = "PublicAddress is a local address";
            Debug.Error($"[Dashboard] PublicAddress {_address} is a local network address, Let's Encrypt needs your public IP or a domain name. HTTPS stays off");
            return;
        }

        _ = RunCertificates(_cancel.Token);
    }

    public void Dispose()
    {
        _cancel.Cancel();
    }

    public static bool NeedsRenewal(string certificateFile, DateTime now, out DateTime expires)
    {
        expires = DateTime.MinValue;
        if (!File.Exists(certificateFile)) return true;

        try
        {
            using var certificate = X509Certificate2.CreateFromPem(File.ReadAllText(certificateFile));
            expires = certificate.NotAfter.ToUniversalTime();
            var lifetime = expires - certificate.NotBefore.ToUniversalTime();
            return expires - now < TimeSpan.FromTicks(lifetime.Ticks / 3);
        }
        catch (Exception)
        {
            return true;
        }
    }

    private async Task RunIpUpdates(CancellationToken token)
    {
        var duckDns = new DuckDnsClient(Http, _config.DuckDnsToken);
        while (!token.IsCancellationRequested)
        {
            try
            {
                await duckDns.UpdateIpAsync(_address, token);
                if (!_ipConfirmed) Debug.Success($"[Dashboard] DuckDNS: {_address} points to this server's public IP");
                _ipConfirmed = true;
                _lastIpError = "";
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                if (ex.Message != _lastIpError) Debug.Error($"[Dashboard] DuckDNS IP update failed: {ex.Message}");
                _lastIpError = ex.Message;
            }

            await Delay(IpUpdateInterval, token);
        }
    }

    private async Task RunCertificates(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var wait = CheckInterval;
            if (NeedsRenewal(CertificateFile, DateTime.UtcNow, out var expires))
                wait = await Issue(token) ? CheckInterval : RetryInterval;
            else
                Status = $"active, valid until {expires:yyyy-MM-dd HH:mm} UTC";

            await Delay(wait, token);
        }
    }

    private IAcmeChallengeSolver CreateSolver() => _method switch
    {
        HttpsMethod.DuckDns => new DnsChallengeSolver(new DuckDnsClient(Http, _config.DuckDnsToken), DuckDnsWait),
        HttpsMethod.Cloudflare => new DnsChallengeSolver(new CloudflareDnsClient(Http, _config.CloudflareApiToken), CloudflareWait),
        _ => SharedHttpSolver ?? new HttpChallengeSolver(_config.HttpsChallengePort)
    };

    private async Task<bool> Issue(CancellationToken token)
    {
        Status = "requesting a certificate";
        string how = HttpsMethodSelector.Describe(_method, _config);
        Debug.Log($"[Dashboard] Requesting an HTTPS certificate for {_address} from Let's Encrypt ({how}){(_config.LetsEncryptStaging ? ", staging" : "")}");

        try
        {
            Directory.CreateDirectory(_folder);
            using var accountKey = LoadOrCreateAccountKey();
            var client = new AcmeClient(Http, _config.LetsEncryptStaging ? AcmeClient.LetsEncryptStaging : AcmeClient.LetsEncrypt, accountKey, message => Debug.Log("[Dashboard] " + message));
            string profile = AcmeClient.IsIpAddress(_address) ? AcmeClient.ShortLivedProfile : null;
            var certificate = await client.IssueAsync(_address, _config.LetsEncryptEmail, CreateSolver(), token, profile);

            WriteAtomic(KeyFile, certificate.PrivateKeyPem);
            WriteAtomic(CertificateFile, certificate.CertificatePem);
            NeedsRenewal(CertificateFile, DateTime.UtcNow, out var expires);
            Status = $"active, valid until {expires:yyyy-MM-dd HH:mm} UTC";
            Debug.Success($"[Dashboard] HTTPS certificate for {_address} saved, valid until {expires:yyyy-MM-dd HH:mm} UTC");
            CertificateChanged?.Invoke();
            return true;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            Status = "failed: " + ex.Message;
            Debug.Error($"[Dashboard] Getting the HTTPS certificate failed: {ex.Message}. {Hint()} Retrying in {(int)RetryInterval.TotalMinutes} minutes");
            return false;
        }
    }

    private string Hint() => _method switch
    {
        HttpsMethod.HttpPort => $"Check that TCP port {_config.HttpsChallengePort} is forwarded to this server and {_address} points to your public IP.",
        HttpsMethod.DuckDns => "Check DuckDnsToken and the DuckDNS name.",
        HttpsMethod.Cloudflare => "Check that CloudflareApiToken may edit the DNS of this domain.",
        _ => ""
    };

    private ECDsa LoadOrCreateAccountKey()
    {
        string path = Path.Combine(Path.GetDirectoryName(_folder), _config.LetsEncryptStaging ? "account-staging.pem" : "account.pem");
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        if (File.Exists(path))
        {
            key.ImportFromPem(File.ReadAllText(path));
            return key;
        }

        WriteAtomic(path, key.ExportPkcs8PrivateKeyPem());
        return key;
    }

    private static void WriteAtomic(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, true);
    }

    private static async Task Delay(TimeSpan wait, CancellationToken token)
    {
        try
        {
            await Task.Delay(wait, token);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
