using Engine.Web;

namespace Spacebox.Network.Dashboard;

public static class DashboardHttpsPolicy
{
    public static bool MustUseHttps(HttpRequest request, bool httpsAvailable)
    {
        return httpsAvailable && !request.IsSecure && !NetworkAddresses.IsLocal(request.RemoteAddress);
    }

    public static HttpResponse Redirect(HttpRequest request)
    {
        if (request.Method != "GET" || string.IsNullOrWhiteSpace(request.Host))
            return HttpResponse.Error("Open the dashboard with https://", 403);

        var response = new HttpResponse { Status = 301 };
        response.Headers["Location"] = $"https://{request.Host}{request.Path}";
        return response;
    }
}
