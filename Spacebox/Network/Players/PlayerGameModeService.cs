using Spacebox.Game.Generation;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public static class PlayerGameModeService
{
    public static GameMode DefaultMode => World.WorldData?.Info?.GameMode ?? GameMode.Survival;

    public static GameMode Of(string name) => PlayerGameModes.Current.Get(name, DefaultMode);

    public static bool Change(string name, GameMode mode)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        PlayerGameModes.Current.Set(name, mode);
        Items.ItemAuthority.OnGameModeChanged(name, mode);
        if (!PlayerRegistry.TryGetByName(name, out var player)) return false;

        player.LastPlayerJson = PlayerGameModes.Current.ApplyTo(player.Name, player.LastPlayerJson);
        var network = AstronautNetwork.Find(player.Peer);
        network?.SetGameModeByServer(mode);
        return network != null;
    }
}
