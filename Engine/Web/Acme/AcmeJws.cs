using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Engine.Web.Acme;

public sealed class AcmeJws
{
    private readonly ECDsa _key;

    public AcmeJws(ECDsa key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        if (_key.KeySize != 256) throw new ArgumentException("The account key must be ECDSA P-256", nameof(key));
    }

    public JsonObject Jwk()
    {
        var parameters = _key.ExportParameters(false);
        return new JsonObject
        {
            ["crv"] = "P-256",
            ["kty"] = "EC",
            ["x"] = Base64Url(parameters.Q.X),
            ["y"] = Base64Url(parameters.Q.Y)
        };
    }

    public string Thumbprint()
    {
        var jwk = Jwk();
        string canonical = $"{{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"{jwk["x"]}\",\"y\":\"{jwk["y"]}\"}}";
        return Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public string KeyAuthorization(string token) => token + "." + Thumbprint();

    public string DnsTxtValue(string token) => Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(KeyAuthorization(token))));

    public string Sign(string url, string nonce, string accountUrl, JsonNode payload)
    {
        var header = new JsonObject { ["alg"] = "ES256", ["nonce"] = nonce, ["url"] = url };
        if (string.IsNullOrEmpty(accountUrl)) header["jwk"] = Jwk();
        else header["kid"] = accountUrl;

        string protectedPart = Base64Url(Encoding.UTF8.GetBytes(header.ToJsonString()));
        string payloadPart = payload == null ? "" : Base64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        byte[] signature = _key.SignData(Encoding.ASCII.GetBytes(protectedPart + "." + payloadPart), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return new JsonObject
        {
            ["protected"] = protectedPart,
            ["payload"] = payloadPart,
            ["signature"] = Base64Url(signature)
        }.ToJsonString();
    }

    public static string Base64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static byte[] FromBase64Url(string text)
    {
        string padded = text.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
