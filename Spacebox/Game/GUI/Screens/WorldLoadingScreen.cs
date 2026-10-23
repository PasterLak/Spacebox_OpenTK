using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class WorldLoadingScreen
{
    public static bool IsActive => GameUi.Current?.Loading.IsActive ?? false;

    public static void Begin(LocalAstronaut player, Action onComplete = null, Func<bool> waitFor = null) => GameUi.Current?.Loading.Begin(player, onComplete, waitFor);
}
