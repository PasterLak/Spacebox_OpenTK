using System.Text;
using System.Text.Json;

namespace Engine.Web;

public sealed class HttpResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public int Status { get; init; } = 200;
    public string ContentType { get; init; } = "text/plain; charset=utf-8";
    public byte[] Body { get; init; } = Array.Empty<byte>();
    public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static HttpResponse Text(string text, int status = 200)
    {
        return new HttpResponse { Status = status, Body = Encoding.UTF8.GetBytes(text ?? "") };
    }

    public static HttpResponse Json(object value, int status = 200)
    {
        var response = new HttpResponse
        {
            Status = status,
            ContentType = "application/json; charset=utf-8",
            Body = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions)
        };
        response.Headers["Cache-Control"] = "no-store";
        return response;
    }

    public static HttpResponse Error(string message, int status)
    {
        return Json(new { error = message }, status);
    }

    public static HttpResponse Bytes(byte[] body, string contentType, int status = 200)
    {
        return new HttpResponse { Status = status, ContentType = contentType, Body = body ?? Array.Empty<byte>() };
    }

    public static HttpResponse Redirect(string location)
    {
        var response = new HttpResponse { Status = 302 };
        response.Headers["Location"] = location;
        return response;
    }

    public static HttpResponse NotFound() => Error("Not found", 404);

    public HttpResponse WithHeader(string name, string value)
    {
        Headers[name] = value;
        return this;
    }

    public static string ReasonPhrase(int status) => status switch
    {
        200 => "OK",
        204 => "No Content",
        302 => "Found",
        304 => "Not Modified",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        409 => "Conflict",
        413 => "Payload Too Large",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        503 => "Service Unavailable",
        _ => "OK"
    };
}
