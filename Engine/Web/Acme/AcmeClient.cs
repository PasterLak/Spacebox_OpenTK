using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;

namespace Engine.Web.Acme;

public interface IDnsChallenge
{
    Task SetTxtAsync(string domain, string value, CancellationToken token);
    Task ClearTxtAsync(string domain, CancellationToken token);
}

public interface IAcmeChallengeSolver
{
    string ChallengeType { get; }
    TimeSpan WaitBeforeValidation { get; }
    Task PrepareAsync(string identifier, string token, string keyAuthorization, string dnsTxtValue, CancellationToken cancel);
    Task CleanupAsync(string identifier, CancellationToken cancel);
}

public sealed record AcmeCertificate(string CertificatePem, string PrivateKeyPem);

public sealed class AcmeException : Exception
{
    public AcmeException(string message) : base(message)
    {
    }
}

public sealed class AcmeClient
{
    public const string LetsEncrypt = "https://acme-v02.api.letsencrypt.org/directory";
    public const string LetsEncryptStaging = "https://acme-staging-v02.api.letsencrypt.org/directory";
    public const string ShortLivedProfile = "shortlived";

    private const string JoseJson = "application/jose+json";
    private const string BadNonce = "urn:ietf:params:acme:error:badNonce";
    private const int MaxPolls = 60;

    private readonly HttpClient _http;
    private readonly string _directoryUrl;
    private readonly AcmeJws _jws;
    private readonly Action<string> _log;
    private readonly TimeSpan _pollInterval;
    private JsonObject _directory;
    private string _nonce;
    private string _accountUrl;

    public AcmeClient(HttpClient http, string directoryUrl, ECDsa accountKey, Action<string> log = null, TimeSpan? pollInterval = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _directoryUrl = directoryUrl;
        _jws = new AcmeJws(accountKey);
        _log = log ?? (_ => { });
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(3);
    }

    public static bool IsIpAddress(string identifier) => IPAddress.TryParse(identifier, out _);

    public async Task<AcmeCertificate> IssueAsync(string identifier, string email, IAcmeChallengeSolver solver, CancellationToken token, string profile = null)
    {
        bool isIp = IPAddress.TryParse(identifier, out var ip);
        _directory = await GetJson(_directoryUrl, token);
        await RegisterAccount(email, token);

        var orderRequest = new JsonObject
        {
            ["identifiers"] = new JsonArray(new JsonObject { ["type"] = isIp ? "ip" : "dns", ["value"] = identifier })
        };
        if (!string.IsNullOrEmpty(profile)) orderRequest["profile"] = profile;

        var (orderUrl, order) = await PostJson(Url("newOrder"), orderRequest, token);
        _log($"Order created for {identifier}");

        try
        {
            foreach (var authorization in order["authorizations"]?.AsArray() ?? new JsonArray())
                await Authorize(identifier, authorization.GetValue<string>(), solver, token);
        }
        finally
        {
            try
            {
                await solver.CleanupAsync(identifier, token);
            }
            catch (Exception ex)
            {
                _log($"Could not clean up the challenge: {ex.Message}");
            }
        }

        using var key = RSA.Create(2048);
        var request = new CertificateRequest(isIp ? new X500DistinguishedName("") : new X500DistinguishedName($"CN={identifier}"), key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        if (isIp) names.AddIpAddress(ip);
        else names.AddDnsName(identifier);
        request.CertificateExtensions.Add(names.Build());

        await PostJson(order["finalize"].GetValue<string>(), new JsonObject { ["csr"] = AcmeJws.Base64Url(request.CreateSigningRequest()) }, token);
        order = await Poll(orderUrl, "valid", "processing", token);

        string certificateUrl = order["certificate"]?.GetValue<string>() ?? throw new AcmeException("The order has no certificate");
        var response = await Post(certificateUrl, null, token, "application/pem-certificate-chain");
        string pem = await response.Content.ReadAsStringAsync(token);
        _log($"Certificate issued for {identifier}");

        return new AcmeCertificate(pem, key.ExportPkcs8PrivateKeyPem());
    }

    private async Task RegisterAccount(string email, CancellationToken token)
    {
        var payload = new JsonObject { ["termsOfServiceAgreed"] = true };
        if (!string.IsNullOrWhiteSpace(email)) payload["contact"] = new JsonArray("mailto:" + email.Trim());

        var response = await Post(Url("newAccount"), payload, token);
        _accountUrl = response.Headers.Location?.ToString() ?? throw new AcmeException("The account has no URL");
    }

    private async Task Authorize(string identifier, string authorizationUrl, IAcmeChallengeSolver solver, CancellationToken token)
    {
        var (_, authorization) = await PostJson(authorizationUrl, null, token);
        if (authorization["status"]?.GetValue<string>() == "valid") return;

        JsonObject challenge = null;
        foreach (var node in authorization["challenges"]?.AsArray() ?? new JsonArray())
        {
            if (node?["type"]?.GetValue<string>() == solver.ChallengeType) challenge = node.AsObject();
        }
        if (challenge == null) throw new AcmeException($"Let's Encrypt offers no {solver.ChallengeType} check for {identifier}");

        string challengeToken = challenge["token"].GetValue<string>();
        await solver.PrepareAsync(identifier, challengeToken, _jws.KeyAuthorization(challengeToken), _jws.DnsTxtValue(challengeToken), token);
        if (solver.WaitBeforeValidation > TimeSpan.Zero)
        {
            _log($"Waiting {(int)solver.WaitBeforeValidation.TotalSeconds} s for the record to spread");
            await Task.Delay(solver.WaitBeforeValidation, token);
        }

        await PostJson(challenge["url"].GetValue<string>(), new JsonObject(), token);
        await Poll(authorizationUrl, "valid", "pending", token);
        _log("Address confirmed");
    }

    private async Task<JsonObject> Poll(string url, string success, string waiting, CancellationToken token)
    {
        for (int i = 0; i < MaxPolls; i++)
        {
            var (_, body) = await PostJson(url, null, token);
            string status = body["status"]?.GetValue<string>();
            if (status == success) return body;
            if (status != waiting && status != "pending" && status != "processing" && status != "ready")
                throw new AcmeException($"Status '{status}': {ProblemText(body)}");

            await Task.Delay(_pollInterval, token);
        }

        throw new AcmeException("Timed out waiting for Let's Encrypt");
    }

    private static string ProblemText(JsonObject body)
    {
        foreach (var challenge in body["challenges"]?.AsArray() ?? new JsonArray())
        {
            string detail = challenge?["error"]?["detail"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(detail)) return detail;
        }
        return body["error"]?["detail"]?.GetValue<string>() ?? "no details";
    }

    private string Url(string name)
    {
        return _directory?[name]?.GetValue<string>() ?? throw new AcmeException($"The directory has no {name}");
    }

    private async Task<JsonObject> GetJson(string url, CancellationToken token)
    {
        using var response = await _http.GetAsync(url, token);
        string text = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode) throw new AcmeException($"{url}: {(int)response.StatusCode}");
        return JsonNode.Parse(text) as JsonObject ?? throw new AcmeException($"{url}: not JSON");
    }

    private async Task<(string Location, JsonObject Body)> PostJson(string url, JsonNode payload, CancellationToken token)
    {
        using var response = await Post(url, payload, token);
        string text = await response.Content.ReadAsStringAsync(token);
        var body = TryParse(text) ?? new JsonObject();
        return (response.Headers.Location?.ToString(), body);
    }

    private async Task<HttpResponseMessage> Post(string url, JsonNode payload, CancellationToken token, string accept = null)
    {
        for (int attempt = 0; ; attempt++)
        {
            if (string.IsNullOrEmpty(_nonce)) await FetchNonce(token);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(_jws.Sign(url, _nonce, _accountUrl, payload), Encoding.UTF8)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(JoseJson);
            if (accept != null) request.Headers.Accept.ParseAdd(accept);

            var response = await _http.SendAsync(request, token);
            _nonce = response.Headers.TryGetValues("Replay-Nonce", out var nonces) ? nonces.FirstOrDefault() : null;
            if (response.IsSuccessStatusCode) return response;

            string text = await response.Content.ReadAsStringAsync(token);
            response.Dispose();
            var problem = TryParse(text);
            if (problem?["type"]?.GetValue<string>() == BadNonce && attempt < 2) continue;

            throw new AcmeException($"{(int)response.StatusCode} {problem?["detail"]?.GetValue<string>() ?? text}");
        }
    }

    private static JsonObject TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private async Task FetchNonce(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, Url("newNonce"));
        using var response = await _http.SendAsync(request, token);
        _nonce = response.Headers.TryGetValues("Replay-Nonce", out var nonces) ? nonces.FirstOrDefault() : null;
        if (string.IsNullOrEmpty(_nonce) || response.StatusCode >= HttpStatusCode.BadRequest) throw new AcmeException("No nonce from the server");
    }
}
