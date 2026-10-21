using System.Globalization;
using Engine.Web;
using OpenTK.Mathematics;

namespace Spacebox.Agent;

public static class AgentQuery
{
    public static string Text(HttpRequest request, string name, string fallback = null)
    {
        string value = request.QueryValue(name);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    public static string RequiredText(HttpRequest request, string name)
    {
        return Text(request, name) ?? throw new AgentRequestException($"Missing query parameter '{name}'");
    }

    public static bool Has(HttpRequest request, string name) => !string.IsNullOrEmpty(request.QueryValue(name));

    public static float Float(HttpRequest request, string name, float fallback)
    {
        string value = request.QueryValue(name);
        if (string.IsNullOrEmpty(value)) return fallback;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) return result;
        throw new AgentRequestException($"Parameter '{name}' is not a number: {value}");
    }

    public static float RequiredFloat(HttpRequest request, string name)
    {
        if (!Has(request, name)) throw new AgentRequestException($"Missing query parameter '{name}'");
        return Float(request, name, 0f);
    }

    public static int Int(HttpRequest request, string name, int fallback)
    {
        return (int)MathF.Round(Float(request, name, fallback));
    }

    public static bool Bool(HttpRequest request, string name, bool fallback = false)
    {
        string value = request.QueryValue(name);
        if (string.IsNullOrEmpty(value)) return fallback;
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasVector(HttpRequest request) => Has(request, "x") && Has(request, "y") && Has(request, "z");

    public static Vector3 Vector(HttpRequest request)
    {
        return new Vector3(RequiredFloat(request, "x"), RequiredFloat(request, "y"), RequiredFloat(request, "z"));
    }

    public static string TextOrBody(HttpRequest request, string name)
    {
        string value = Text(request, name);
        if (value != null) return value;
        string body = request.BodyText;
        return string.IsNullOrEmpty(body) ? throw new AgentRequestException($"Pass '{name}' as a query parameter or as the request body") : body;
    }
}
