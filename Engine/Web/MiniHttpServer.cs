using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Engine.Web;

public sealed class MiniHttpServer : IDisposable
{
    private const int MaxConnections = 32;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly Func<HttpRequest, Task<HttpResponse>> _handler;
    private readonly int _maxBodyBytes;
    private readonly SemaphoreSlim _connections = new SemaphoreSlim(MaxConnections);
    private readonly List<TcpListener> _listeners = new List<TcpListener>();
    private CancellationTokenSource _cancel;

    private const byte TlsHandshake = 0x16;

    public Func<X509Certificate2> CertificateProvider { get; set; }
    public int Port { get; private set; }
    public bool IsRunning => _listeners.Count > 0;

    public MiniHttpServer(Func<HttpRequest, Task<HttpResponse>> handler, int maxBodyBytes = 1024 * 1024)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _maxBodyBytes = maxBodyBytes;
    }

    public void Start(int port)
    {
        if (IsRunning) return;

        _cancel = new CancellationTokenSource();

        var ipv4 = new TcpListener(IPAddress.Any, port);
        ipv4.Start();
        Port = ((IPEndPoint)ipv4.LocalEndpoint).Port;
        _listeners.Add(ipv4);

        var ipv6 = TryStartIPv6(Port);
        if (ipv6 != null) _listeners.Add(ipv6);

        foreach (var listener in _listeners) _ = AcceptLoop(listener, _cancel.Token);
    }

    public void Stop()
    {
        _cancel?.Cancel();
        foreach (var listener in _listeners)
        {
            try
            {
                listener.Stop();
            }
            catch (SocketException)
            {
            }
        }
        _listeners.Clear();
    }

    public void Dispose() => Stop();

    private static TcpListener TryStartIPv6(int port)
    {
        if (!Socket.OSSupportsIPv6) return null;

        try
        {
            var listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);
            listener.Start();
            return listener;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private async Task AcceptLoop(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
            }
            catch (Exception)
            {
                if (token.IsCancellationRequested) return;
                continue;
            }

            if (!await _connections.WaitAsync(0, token))
            {
                client.Dispose();
                continue;
            }

            _ = Serve(client, token);
        }
    }

    private async Task Serve(TcpClient client, CancellationToken serverToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
        timeout.CancelAfter(RequestTimeout);

        try
        {
            using (client)
            {
                Stream stream = client.GetStream();
                var first = new byte[1];
                if (await stream.ReadAsync(first, timeout.Token) == 0) return;
                stream = new PrefixedStream(stream, first);

                bool secure = false;
                if (first[0] == TlsHandshake)
                {
                    var certificate = CertificateProvider?.Invoke();
                    if (certificate == null) return;

                    var ssl = new SslStream(stream, false);
                    await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificateRequired = false
                    }, timeout.Token);
                    stream = ssl;
                    secure = true;
                }

                await using var connection = stream;
                string remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address is { } address
                    ? (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString()
                    : "";

                HttpResponse response;
                try
                {
                    var request = await HttpProtocol.ReadRequestAsync(stream, remote, _maxBodyBytes, timeout.Token, secure);
                    if (request == null) return;
                    response = await _handler(request);
                }
                catch (PayloadTooLargeException)
                {
                    response = HttpResponse.Error("Request too large", 413);
                }
                catch (InvalidDataException)
                {
                    response = HttpResponse.Error("Bad request", 400);
                }

                await HttpProtocol.WriteResponseAsync(stream, response ?? HttpResponse.NotFound(), timeout.Token);
            }
        }
        catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is SocketException || ex is ObjectDisposedException || ex is AuthenticationException)
        {
        }
        catch (Exception ex)
        {
            Debug.Error($"[Http] {ex.Message}");
        }
        finally
        {
            _connections.Release();
        }
    }
}
