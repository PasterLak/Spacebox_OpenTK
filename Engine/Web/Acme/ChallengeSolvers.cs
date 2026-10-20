using System.Text;

namespace Engine.Web.Acme;

public sealed class DnsChallengeSolver : IAcmeChallengeSolver
{
    private readonly IDnsChallenge _dns;

    public DnsChallengeSolver(IDnsChallenge dns, TimeSpan waitBeforeValidation)
    {
        _dns = dns ?? throw new ArgumentNullException(nameof(dns));
        WaitBeforeValidation = waitBeforeValidation;
    }

    public string ChallengeType => "dns-01";
    public TimeSpan WaitBeforeValidation { get; }

    public Task PrepareAsync(string identifier, string token, string keyAuthorization, string dnsTxtValue, CancellationToken cancel)
    {
        return _dns.SetTxtAsync(identifier, dnsTxtValue, cancel);
    }

    public Task CleanupAsync(string identifier, CancellationToken cancel)
    {
        return _dns.ClearTxtAsync(identifier, cancel);
    }
}

public sealed class HttpChallengeSolver : IAcmeChallengeSolver
{
    public const int DefaultPort = 80;
    public const string PathPrefix = "/.well-known/acme-challenge/";

    private readonly int _port;
    private readonly bool _ownServer;
    private readonly Dictionary<string, string> _answers = new Dictionary<string, string>();
    private MiniHttpServer _server;

    public HttpChallengeSolver(int port = DefaultPort, bool ownServer = true)
    {
        _port = port;
        _ownServer = ownServer;
    }

    public string ChallengeType => "http-01";
    public TimeSpan WaitBeforeValidation => TimeSpan.Zero;
    public int Port => _server?.Port ?? _port;

    public Task PrepareAsync(string identifier, string token, string keyAuthorization, string dnsTxtValue, CancellationToken cancel)
    {
        lock (_answers) _answers[token] = keyAuthorization;

        if (_ownServer && _server == null)
        {
            _server = new MiniHttpServer(Answer, 1024);
            try
            {
                _server.Start(_port);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _server = null;
                throw new AcmeException($"TCP port {_port} is needed for the check but could not be opened ({ex.Message})");
            }
        }

        return Task.CompletedTask;
    }

    public Task CleanupAsync(string identifier, CancellationToken cancel)
    {
        _server?.Stop();
        _server = null;
        lock (_answers) _answers.Clear();
        return Task.CompletedTask;
    }

    public static bool IsChallengePath(string path) => path != null && path.StartsWith(PathPrefix, StringComparison.Ordinal);

    public HttpResponse Answer(string path)
    {
        if (IsChallengePath(path))
        {
            lock (_answers)
            {
                if (_answers.TryGetValue(path.Substring(PathPrefix.Length), out var answer))
                    return HttpResponse.Bytes(Encoding.ASCII.GetBytes(answer), "application/octet-stream");
            }
        }

        return HttpResponse.NotFound();
    }

    private Task<HttpResponse> Answer(HttpRequest request)
    {
        return Task.FromResult(request.Method == "GET" ? Answer(request.Path) : HttpResponse.NotFound());
    }
}
