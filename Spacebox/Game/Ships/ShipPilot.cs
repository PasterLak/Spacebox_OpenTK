using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.Ships;

public static class ShipPilot
{
    private static LocalShipPilot Local => GameScene.Current?.Pilot;

    public static ShipConsoleBlock Console => Local?.Console;
    public static bool IsPiloting => Local?.IsPiloting == true;
    public static bool IsUsingCamera => Local?.IsUsingCamera == true;
    public static bool AllowsMovement => Local?.AllowsMovement == true;
    public static bool IsSeated => Local?.IsSeated == true;

    public static void Mount(ShipConsoleBlock console, LocalAstronaut player) => Local?.Mount(console, player);

    public static void Unmount(LocalAstronaut player) => Local?.Unmount(player);

    public static void OnConsoleUnavailable(ShipConsoleBlock console) => Local?.OnConsoleUnavailable(console);

    public static void Update(LocalAstronaut player) => Local?.Update(player);

    public static void UpdateViewCamera(LocalAstronaut player) => Local?.UpdateViewCamera(player);
}
