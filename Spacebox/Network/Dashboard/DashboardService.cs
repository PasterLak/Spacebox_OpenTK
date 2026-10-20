using System.Net.Sockets;
using Engine;
using Engine.Web;
using Spacebox.Network.Dashboard.Modules;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network.Dashboard;

public static class DashboardService
{
    public const int MaxBodyBytes = 512 * 1024;

    private static MiniHttpServer _server;
    private static DashboardMetrics _metrics;
    private static DashboardLog _log;
    private static DashboardCertificate _certificate;
    private static AutoCertificateService _autoCertificate;
    private static ServerConfig _config;

    public static string HttpsStatus
    {
        get
        {
            if (_autoCertificate != null && AutoCertificateService.IsConfigured(_config)) return $"{_autoCertificate.Domain}: {_autoCertificate.Status} ({HttpsMethodSelector.Describe(_autoCertificate.Method, _config)})";
            if (_certificate?.IsConfigured != true) return "off (plain HTTP)";
            var current = _certificate.Current;
            return current != null ? $"active, valid until {current.NotAfter:yyyy-MM-dd}" : "certificate error: " + _certificate.LastError;
        }
    }

    public static bool IsRunning => _server?.IsRunning == true;

    public static void Start(ServerConfig config)
    {
        if (config == null || !config.DashboardEnabled || IsRunning) return;

        _config = config;
        _log = new DashboardLog();
        Debug.MessageLogged += _log.Append;

        var sessions = new DashboardSessions();
        var admins = new DashboardAdminStore(DashboardPaths.AdminFile, DashboardPaths.SetupKeyFile);
        _metrics = new DashboardMetrics(() => PlayerRegistry.All.Count);

        var context = new DashboardContext
        {
            Config = config,
            Admins = admins,
            Sessions = sessions,
            Throttle = new LoginThrottle(),
            Guard = new DashboardGuard(sessions),
            Log = _log,
            Metrics = _metrics,
            Icons = new DashboardItemIcons(GameSetLocator.LocalDirectory)
        };

        var router = new HttpRouter();
        IDashboardModule[] modules =
        {
            new StaticFilesModule(DashboardPaths.WebFolder, DashboardPaths.FontFile),
            new AuthModule(),
            new ServerModule(),
            new PlayersModule(),
            new ModerationModule(),
            new ConsoleModule()
        };
        foreach (var module in modules) module.Map(router, context);
        router.MapPrefix("GET", Engine.Web.Acme.HttpChallengeSolver.PathPrefix, (request, _) =>
            _autoCertificate?.SharedHttpSolver?.Answer(request.Path) ?? HttpResponse.NotFound());

        int port = config.GetDashboardPort();
        _certificate = CreateCertificate(config, port);
        _server = new MiniHttpServer(request => Handle(router, request), MaxBodyBytes);
        if (_certificate.IsConfigured) _server.CertificateProvider = () => _certificate.Current;
        try
        {
            _server.Start(port);
        }
        catch (SocketException ex)
        {
            Debug.Error($"[Dashboard] Could not start on TCP port {port}: {ex.Message}");
            _server = null;
            return;
        }

        _metrics.Start();
        _autoCertificate?.Start();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Stop();

        if (AutoCertificateService.IsConfigured(config) && _certificate.Current == null)
            Debug.Log($"[Dashboard] HTTPS for https://{_autoCertificate.Domain}:{port}{DashboardPaths.Route} is being set up automatically ({HttpsMethodSelector.Describe(_autoCertificate.Method, config)}), until then use http://<server address>:{port}{DashboardPaths.Route}");
        else if (_certificate.Current != null)
            Debug.Success($"[Dashboard] Open https://{string.Join(" or https://", _certificate.DnsNames())}:{port}{DashboardPaths.Route} (plain http only from the local network)");
        else
            Debug.Success($"[Dashboard] Open http://<server address>:{port}{DashboardPaths.Route}");
        string key = admins.EnsureSetupKey();
        if (key != null)
            Debug.Warning($"[Dashboard] No admin yet. Register in the dashboard with the setup key from {DashboardPaths.SetupKeyFile}");
    }

    private static DashboardCertificate CreateCertificate(ServerConfig config, int dashboardPort)
    {
        bool automatic = AutoCertificateService.IsConfigured(config);
        if (automatic || HttpsMethodSelector.UpdatesDuckDnsIp(config))
            _autoCertificate = new AutoCertificateService(config, DashboardPaths.CertificatesFolder);

        if (!automatic)
            return new DashboardCertificate(config.DashboardCertificateFile, config.DashboardCertificateKeyFile, config.DashboardCertificatePassword);

        if (_autoCertificate.Method == HttpsMethod.HttpPort && config.HttpsChallengePort == dashboardPort)
            _autoCertificate.SharedHttpSolver = new Engine.Web.Acme.HttpChallengeSolver(dashboardPort, ownServer: false);

        var certificate = new DashboardCertificate(_autoCertificate.CertificateFile, _autoCertificate.KeyFile, "", reportMissing: false);
        _autoCertificate.CertificateChanged += certificate.Refresh;
        return certificate;
    }

    public static void Stop()
    {
        _autoCertificate?.Dispose();
        _autoCertificate = null;
        _server?.Stop();
        _server = null;
        _certificate = null;
        _metrics?.Dispose();
        _metrics = null;
        if (_log != null) Debug.MessageLogged -= _log.Append;
        _log = null;
    }

    private static async Task<HttpResponse> Handle(HttpRouter router, HttpRequest request)
    {
        HttpResponse response;
        try
        {
            response = DashboardHttpsPolicy.MustUseHttps(request, _certificate?.Current != null) && !Engine.Web.Acme.HttpChallengeSolver.IsChallengePath(request.Path)
                ? DashboardHttpsPolicy.Redirect(request)
                : await router.Handle(request);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Dashboard] {request.Method} {request.Path}: {ex.Message}");
            response = HttpResponse.Error("Server error", 500);
        }

        if (request.IsSecure) response.Headers["Strict-Transport-Security"] = "max-age=31536000";
        return AddSecurityHeaders(response);
    }

    public static HttpResponse AddSecurityHeaders(HttpResponse response)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob:; style-src 'self'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        return response;
    }
}
