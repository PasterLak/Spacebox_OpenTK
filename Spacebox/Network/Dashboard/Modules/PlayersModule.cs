using Engine;
using Engine.Networking;
using Engine.Web;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Network.Moderation;
using Spacebox.Network.Players;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class PlayersModule : IDashboardModule
{
    private sealed class MuteRequest
    {
        public string Name { get; set; }
        public bool Muted { get; set; }
    }

    private sealed class GameModeRequest
    {
        public string Name { get; set; }
        public int Mode { get; set; }
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        var guard = context.Guard;

        router.Map("GET", DashboardContext.Api("/players"), guard.Admin(async (_, _) => HttpResponse.Json(await MainThread.Run(() => List(context.PlayTimes)))));

        router.Map("GET", DashboardContext.Api("/players/detail"), guard.Admin(async (request, _) =>
        {
            string name = request.QueryValue("name");
            if (string.IsNullOrWhiteSpace(name)) return HttpResponse.Error("Missing name", 400);

            var detail = await MainThread.Run(() => Detail(name));
            return detail == null ? HttpResponse.Error("No save for this player", 404) : HttpResponse.Json(detail);
        }));

        router.Map("GET", DashboardContext.Api("/items/icon"), guard.Admin((request, _) =>
        {
            var icon = context.Icons.Get(request.QueryValue("id"));
            return icon == null ? HttpResponse.NotFound() : HttpResponse.Bytes(icon, "image/png").WithHeader("Cache-Control", "max-age=3600");
        }));

        router.Map("POST", DashboardContext.Api("/players/kick"), guard.Admin((request, user) => OnlineAction(request, user, "kicked", ServerModeration.Kick)));
        router.Map("POST", DashboardContext.Api("/players/ban"), guard.Admin(async (request, user) =>
        {
            string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

            bool banned = await MainThread.Run(() => ServerModeration.BanName(name));
            if (banned) DashboardContext.Audit(user, $"banned {name}");
            return HttpResponse.Json(new { ok = banned });
        }));

        router.Map("POST", DashboardContext.Api("/players/gamemode"), guard.Admin(async (request, user) =>
        {
            var body = DashboardGuard.Read<GameModeRequest>(request);
            string name = body?.Name?.Trim();
            if (string.IsNullOrEmpty(name) || body.Mode < 0 || !Spacebox.Game.Player.GameModes.GameModeCatalog.Contains((Spacebox.Game.Player.GameModes.GameMode)body.Mode))
                return HttpResponse.Error("Bad request", 400);

            var mode = (Spacebox.Game.Player.GameModes.GameMode)body.Mode;
            bool applied = await MainThread.Run(() => PlayerGameModeService.Change(name, mode));
            DashboardContext.Audit(user, $"set the game mode of {name} to {Spacebox.Game.Player.GameModes.GameModeCatalog.NameOf(mode)}");
            return HttpResponse.Json(new { ok = true, applied });
        }));

        router.Map("POST", DashboardContext.Api("/players/mute"), guard.Admin(async (request, user) =>
        {
            var body = DashboardGuard.Read<MuteRequest>(request);
            string name = body?.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

            await MainThread.Run(() => ServerModeration.SetMuted(name, body.Muted));
            DashboardContext.Audit(user, $"{(body.Muted ? "muted" : "unmuted")} {name}");
            return HttpResponse.Json(new { ok = true });
        }));
    }

    private static async Task<HttpResponse> OnlineAction(HttpRequest request, string user, string verb, Action<int> action)
    {
        string name = DashboardGuard.Read<NameRequest>(request)?.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return HttpResponse.Error("Missing name", 400);

        bool found = await MainThread.Run(() =>
        {
            if (!PlayerRegistry.TryGetByName(name, out var player)) return false;
            action(player.Peer);
            return true;
        });

        if (!found) return HttpResponse.Error("The player is not online", 404);
        DashboardContext.Audit(user, $"{verb} {name}");
        return HttpResponse.Json(new { ok = true });
    }

    private static object List(PlayerPlayTimes playTimes)
    {
        var network = NetworkManager.Instance;
        var online = new List<object>();
        var onlineNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var player in PlayerRegistry.All)
        {
            onlineNames.Add(player.Name);
            bool hasPose = PlayerLocator.TryGetPose(player.Peer, out var position, out _);
            online.Add(new
            {
                name = player.Name,
                peer = player.Peer,
                ping = network.GetRoundTripMilliseconds(player.Peer),
                address = network.GetPeerAddress(player.Peer),
                position = hasPose ? new[] { MathF.Round(position.X, 1), MathF.Round(position.Y, 1), MathF.Round(position.Z, 1) } : null,
                alive = AstronautNetwork.Find(player.Peer)?.IsAlive ?? false,
                muted = ServerModeration.IsMuted(player.Name),
                gameMode = (int)PlayerGameModeService.IdOf(player.Name),
                skin = player.SkinColor,
                sessionSeconds = Math.Round(Math.Max(0, (DateTime.UtcNow - player.JoinedAtUtc).TotalSeconds))
            });
        }

        var saved = new List<object>();
        string folder = World.WorldData?.WorldFolderPath;
        if (folder != null)
        {
            foreach (var name in PlayerFiles.SavedPlayerNames(folder))
            {
                var played = playTimes.Get(folder, name);
                saved.Add(new
                {
                    name,
                    online = onlineNames.Contains(name),
                    lastSeen = PlayerFiles.LastSaved(folder, name).ToString("yyyy-MM-dd HH:mm"),
                    playSeconds = Math.Round(played.Seconds),
                    sessions = played.Sessions,
                    averageSessionSeconds = Math.Round(played.AverageSessionSeconds),
                    muted = ServerModeration.IsMuted(name),
                    gameMode = (int)PlayerGameModeService.IdOf(name)
                });
            }
        }

        var modes = new List<object>();
        foreach (var mode in Spacebox.Game.Player.GameModes.GameModeCatalog.All) modes.Add(new { id = (int)mode.Id, name = mode.Name });

        return new { online, saved, gameModes = modes };
    }

    private static object Detail(string name)
    {
        string json = PlayerRegistry.TryGetByName(name, out var player) && !string.IsNullOrEmpty(player.LastPlayerJson)
            ? player.LastPlayerJson
            : World.WorldData == null ? null : PlayerSaveLoadManager.ReadPlayerJson(World.WorldData.WorldFolderPath, name, false);

        var view = PlayerSaveView.Parse(json);
        if (view == null) return null;

        foreach (var slot in view.AllSlots)
            slot.Name = GameAssets.TryGetItemByFullID(slot.Id, out var item) ? item.Name : slot.Id;

        return new
        {
            name,
            online = player != null,
            muted = ServerModeration.IsMuted(name),
            gameMode = (int)PlayerGameModeService.IdOf(name),
            view.Health,
            view.Power,
            view.Oxygen,
            view.Position,
            view.Inventory,
            view.Panel,
            view.Equipment,
            view.Statistics
        };
    }
}
