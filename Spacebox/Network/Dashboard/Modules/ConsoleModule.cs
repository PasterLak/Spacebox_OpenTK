using Engine;
using Engine.Commands;
using Engine.Web;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class ConsoleModule : IDashboardModule
{
    public const int MaxCommandLength = 512;

    private sealed class CommandRequest
    {
        public string Command { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;

        router.Map("GET", DashboardContext.Api("/console"), guard.Admin((request, _) =>
        {
            long.TryParse(request.QueryValue("after"), out long after);
            return HttpResponse.Json(new { lines = context.Log.After(after), last = context.Log.LastId });
        }));

        router.Map("GET", DashboardContext.Api("/commands"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() =>
            CommandManager.GetCommands()
                .Where(CommandManager.IsAvailable)
                .Select(command => new { name = command.Name, description = command.Description })
                .OrderBy(command => command.name)
                .ToList()))));

        router.Map("POST", DashboardContext.Api("/console"), guard.Admin(async (request, user) =>
        {
            string command = StripControlCharacters(DashboardGuard.Read<CommandRequest>(request)?.Command)?.Trim();
            if (string.IsNullOrEmpty(command)) return HttpResponse.Error("Enter a command", 400);
            if (command.Length > MaxCommandLength) return HttpResponse.Error("The command is too long", 400);

            await MainThread.Run(() =>
            {
                Debug.Log($"[Dashboard] {user}> {command}");
                Debug.ExecuteCommand(command);
            });
            return HttpResponse.Json(new { ok = true, last = context.Log.LastId });
        }));
    }

    public static string StripControlCharacters(string text)
    {
        if (text == null) return null;

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
            builder.Append(char.IsControl(c) ? ' ' : c);
        return builder.ToString();
    }
}
