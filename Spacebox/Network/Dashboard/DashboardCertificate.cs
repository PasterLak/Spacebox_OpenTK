using System.Security.Cryptography.X509Certificates;
using Engine;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardCertificate
{
    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ExpiryWarning = TimeSpan.FromDays(14);

    private readonly string _certificateFile;
    private readonly string _keyFile;
    private readonly string _password;
    private readonly Func<DateTime> _clock;
    private readonly object _lock = new object();
    private X509Certificate2 _certificate;
    private DateTime _certificateWrite;
    private DateTime _keyWrite;
    private DateTime _nextCheck;

    private readonly bool _reportMissing;

    public DashboardCertificate(string certificateFile, string keyFile, string password, Func<DateTime> clock = null, bool reportMissing = true)
    {
        _reportMissing = reportMissing;
        _certificateFile = Resolve(certificateFile);
        _keyFile = Resolve(keyFile);
        _password = password ?? "";
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_certificateFile);
    public string LastError { get; private set; }

    public X509Certificate2 Current
    {
        get
        {
            if (!IsConfigured) return null;

            lock (_lock)
            {
                if (_clock() >= _nextCheck)
                {
                    _nextCheck = _clock() + CheckInterval;
                    ReloadIfChanged();
                }
                return _certificate;
            }
        }
    }

    public void Refresh()
    {
        lock (_lock) _nextCheck = DateTime.MinValue;
    }

    public IReadOnlyList<string> DnsNames()
    {
        var certificate = Current;
        if (certificate == null) return Array.Empty<string>();

        var names = new List<string>();
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509SubjectAlternativeNameExtension alternative)
                names.AddRange(alternative.EnumerateDnsNames());
        }
        if (names.Count == 0) names.Add(certificate.GetNameInfo(X509NameType.SimpleName, false));
        return names;
    }

    private void ReloadIfChanged()
    {
        var certificateWrite = WriteTime(_certificateFile);
        var keyWrite = WriteTime(_keyFile);
        if (_certificate != null && certificateWrite == _certificateWrite && keyWrite == _keyWrite) return;

        var loaded = Load(out var error);
        if (loaded == null)
        {
            bool quiet = !_reportMissing && !File.Exists(_certificateFile);
            if (error != LastError && !quiet) Debug.Error($"[Dashboard] HTTPS certificate: {error}");
            LastError = error;
            return;
        }

        _certificate?.Dispose();
        _certificate = loaded;
        _certificateWrite = certificateWrite;
        _keyWrite = keyWrite;
        LastError = null;

        var left = loaded.NotAfter.ToUniversalTime() - _clock();
        Debug.Success($"[Dashboard] HTTPS certificate loaded: {loaded.GetNameInfo(X509NameType.SimpleName, false)}, valid until {loaded.NotAfter:yyyy-MM-dd}");
        if (left < ExpiryWarning) Debug.Warning($"[Dashboard] The HTTPS certificate expires in {Math.Max(0, (int)left.TotalDays)} days, renew it");
    }

    private X509Certificate2 Load(out string error)
    {
        error = null;
        if (!File.Exists(_certificateFile))
        {
            error = $"file not found: {_certificateFile}";
            return null;
        }

        try
        {
            X509Certificate2 certificate;
            if (IsPfx(_certificateFile))
            {
                certificate = new X509Certificate2(_certificateFile, _password, X509KeyStorageFlags.Exportable);
            }
            else
            {
                if (string.IsNullOrEmpty(_keyFile) || !File.Exists(_keyFile))
                {
                    error = $"private key file not found: {_keyFile}";
                    return null;
                }

                using var pem = X509Certificate2.CreateFromPemFile(_certificateFile, _keyFile);
                certificate = new X509Certificate2(pem.Export(X509ContentType.Pkcs12), (string)null, X509KeyStorageFlags.Exportable);
            }

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                error = "the certificate has no private key";
                return null;
            }

            return certificate;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private static bool IsPfx(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".p12", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime WriteTime(string path)
    {
        return !string.IsNullOrEmpty(path) && File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
    }

    private static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
