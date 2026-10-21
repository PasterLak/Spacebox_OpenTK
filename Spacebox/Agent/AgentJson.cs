using System.Text.Encodings.Web;
using System.Text.Json;
using Engine.Web;

namespace Spacebox.Agent;

public static class AgentJson
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static HttpResponse Response(object value, int status = 200)
    {
        return HttpResponse.Bytes(JsonSerializer.SerializeToUtf8Bytes(value, Options), "application/json; charset=utf-8", status).WithHeader("Cache-Control", "no-store");
    }
}
