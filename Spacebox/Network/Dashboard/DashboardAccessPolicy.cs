using System.Net;
using Engine.Web;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardAccessPolicy
{
    private static readonly string[] LocalSuffixes = { ".local", ".lan", ".home", ".internal", ".home.arpa", ".localhost" };

    private readonly string _publicAddress;
    private readonly bool _allowPublicHttp;
    private readonly Func<IReadOnlyList<string>> _certificateNames;

    public DashboardAccessPolicy(string publicAddress, bool allowPublicHttp, Func<IReadOnlyList<string>> certificateNames = null)
    {
        _publicAddress = HttpsMethodSelector.Normalize(publicAddress);
        _allowPublicHttp = allowPublicHttp;
        _certificateNames = certificateNames ?? (() => Array.Empty<string>());
    }

    public HttpResponse Check(HttpRequest request, bool httpsAvailable)
    {
        if (!IsAllowedHost(request.Host))
            return HttpResponse.Error("Unknown host name. Add it as PublicAddress in serverconfig.json", 421);

        if (request.IsSecure || NetworkAddresses.IsLocal(request.RemoteAddress)) return null;
        if (httpsAvailable) return DashboardHttpsPolicy.Redirect(request);
        if (!_allowPublicHttp) return HttpResponse.Error("The dashboard is only reachable over plain HTTP from the local network. Set up HTTPS or DashboardAllowPublicHttp", 403);
        return null;
    }

    public bool IsAllowedHost(string hostHeader)
    {
        if (string.IsNullOrWhiteSpace(hostHeader)) return true;

        string host = HttpsMethodSelector.Normalize(hostHeader).Trim('[', ']');
        if (host.Length == 0) return false;
        if (IPAddress.TryParse(host, out _) || host == "localhost" || host.IndexOf('.') < 0) return true;
        if (host == _publicAddress) return true;

        foreach (string suffix in LocalSuffixes)
            if (host.EndsWith(suffix, StringComparison.Ordinal)) return true;

        foreach (string name in _certificateNames())
            if (Matches(host, name)) return true;

        return false;
    }

    private static bool Matches(string host, string certificateName)
    {
        string name = (certificateName ?? "").ToLowerInvariant().TrimEnd('.');
        if (name.StartsWith("*.", StringComparison.Ordinal))
        {
            string parent = name.Substring(1);
            return host.EndsWith(parent, StringComparison.Ordinal) && host.IndexOf('.') == host.Length - parent.Length;
        }
        return host == name;
    }
}
