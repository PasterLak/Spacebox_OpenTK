using Engine.Web;

namespace Spacebox.Network.Dashboard;

public static class DashboardHttpsPolicy
{
    public static HttpResponse Redirect(HttpRequest request)
    {
        if (request.Method != "GET" || string.IsNullOrWhiteSpace(request.Host) || request.Host.IndexOfAny(new[] { '/', '\\', '@', ' ' }) >= 0)
            return HttpResponse.Error("Open the dashboard with https://", 403);

        var response = new HttpResponse { Status = 307 };
        response.Headers["Location"] = $"https://{request.Host}{request.Path}";
        return response;
    }
}
