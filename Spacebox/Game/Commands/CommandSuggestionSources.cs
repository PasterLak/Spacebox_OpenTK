using Engine.Networking;
using Spacebox.Game.Resource;
using Spacebox.Network;

namespace Spacebox.Game.Commands;

public static class CommandSuggestionSources
{
    public static void AddPlayerNames(List<string> results)
    {
        if (GameSession.HasLocalPlayer && !string.IsNullOrEmpty(GameSession.PlayerName)) results.Add(GameSession.PlayerName);

        if (NetworkManager.Instance.IsServer)
        {
            foreach (var player in PlayerRegistry.All) results.Add(player.Name);
            return;
        }

        var players = PlayerListNetwork.Players;
        for (int i = 0; i < players.Count; i++) results.Add(players[i].Name);
    }

    public static void AddItemIds(List<string> results)
    {
        string modId = GameSetLoader.ModInfo?.ModId ?? "";
        foreach (var fullId in GameAssets.ItemsStr.Keys) results.Add(ItemLookup.DisplayId(fullId, modId));
    }

    public static void AddMobIds(List<string> results)
    {
        foreach (var id in GameAssets.Mobs.Keys) results.Add(id);
    }

    public static void AddGameModes(List<string> results)
    {
        foreach (var mode in Spacebox.Game.Player.GameModes.GameModeCatalog.All) results.Add(mode.Key);
    }
}
