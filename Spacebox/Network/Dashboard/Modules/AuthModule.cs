using Engine;
using Engine.Web;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class AuthModule : IDashboardModule
{
    private sealed class SetupRequest
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
    }

    private sealed class LoginRequest
    {
        public string Name { get; set; }
        public string Password { get; set; }
    }

    private sealed class PasswordRequest
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        router.Map("GET", DashboardContext.Api("/state"), request =>
        {
            bool loggedIn = context.Guard.TryGetUser(request, out var user);
            return HttpResponse.Json(new
            {
                setupRequired = !context.Admins.HasAdmin,
                loggedIn,
                user = loggedIn ? user : null,
                serverName = context.Config.ServerName,
                version = loggedIn ? Application.Version : null
            });
        });

        router.Map("POST", DashboardContext.Api("/setup"), request => Setup(request, context));
        router.Map("POST", DashboardContext.Api("/login"), request => Login(request, context));

        router.Map("POST", DashboardContext.Api("/logout"), request =>
        {
            if (!DashboardGuard.HasRequestHeader(request)) return HttpResponse.Error("Missing request header", 403);
            context.Sessions.Remove(request.Cookie(DashboardSessions.CookieName));
            return HttpResponse.Json(new { ok = true }).WithHeader("Set-Cookie", DashboardSessions.ExpiredCookieHeader());
        });

        router.Map("POST", DashboardContext.Api("/password"), context.Guard.Admin((request, user) =>
        {
            var body = DashboardGuard.Read<PasswordRequest>(request);
            if (body == null) return HttpResponse.Error("Bad request", 400);
            if (!context.Throttle.TryBeginSessionAttempt(request.Cookie(DashboardSessions.CookieName))) return HttpResponse.Error("Too many attempts, wait a few minutes", 429);
            if (!context.Admins.TryChangePassword(body.OldPassword, body.NewPassword, out var error)) return HttpResponse.Error(error, 400);

            context.Sessions.Clear();
            DashboardContext.Audit(user, "changed the dashboard password");
            return HttpResponse.Json(new { ok = true }).WithHeader("Set-Cookie", DashboardSessions.ExpiredCookieHeader());
        }));
    }

    private static HttpResponse Setup(HttpRequest request, DashboardContext context)
    {
        if (!DashboardGuard.HasRequestHeader(request)) return HttpResponse.Error("Missing request header", 403);
        if (context.Admins.HasAdmin) return HttpResponse.Error("An admin is already registered", 409);
        var body = DashboardGuard.Read<SetupRequest>(request);
        if (body == null) return HttpResponse.Error("Bad request", 400);
        if (!context.Throttle.TryBeginAttempt(request.RemoteAddress)) return HttpResponse.Error("Too many attempts, wait a few minutes", 429);

        if (!context.Admins.TryRegister(body.Key, body.Name, body.Password, out var error)) return HttpResponse.Error(error, 400);

        context.Throttle.RecordSuccess(request.RemoteAddress);
        Debug.Success($"[Dashboard] Admin '{context.Admins.AdminName}' registered from {request.RemoteAddress}");
        return SignIn(request, context, context.Admins.AdminName);
    }

    private static HttpResponse Login(HttpRequest request, DashboardContext context)
    {
        if (!DashboardGuard.HasRequestHeader(request)) return HttpResponse.Error("Missing request header", 403);
        if (!context.Throttle.TryBeginAttempt(request.RemoteAddress)) return HttpResponse.Error("Too many attempts, wait a few minutes", 429);

        var body = DashboardGuard.Read<LoginRequest>(request);
        if (body == null || !context.Admins.Verify(body.Name, body.Password))
        {
            Debug.Warning($"[Dashboard] Failed login from {request.RemoteAddress}");
            return HttpResponse.Error("Wrong name or password", 401);
        }

        context.Throttle.RecordSuccess(request.RemoteAddress);
        Debug.Log($"[Dashboard] {context.Admins.AdminName} logged in from {request.RemoteAddress}");
        return SignIn(request, context, context.Admins.AdminName);
    }

    private static HttpResponse SignIn(HttpRequest request, DashboardContext context, string user)
    {
        string token = context.Sessions.Create(user);
        return HttpResponse.Json(new { ok = true, user }).WithHeader("Set-Cookie", DashboardSessions.CookieHeader(token, request.IsSecure));
    }
}
