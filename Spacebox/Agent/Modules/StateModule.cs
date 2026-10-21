using Engine;
using Engine.SceneManagement;
using Engine.Web;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Agent.Modules;

public sealed class StateModule : IAgentModule
{
    private readonly NavigationModule _navigation;

    public StateModule(NavigationModule navigation)
    {
        _navigation = navigation;
    }

    public static bool IsWorldReady => AgentGame.Player != null && !WorldLoadingScreen.IsActive;

    public void Map(AgentRouter router)
    {
        router.Get("/state", "Scene, loading, cursor, open UI windows, player stats, crosshair target and navigation", _ => AgentJson.Response(Build()));
    }

    private object Build()
    {
        var player = AgentGame.Player;
        return new
        {
            scene = SceneManager.Current?.GetType().Name,
            worldReady = IsWorldReady,
            loading = WorldLoadingScreen.IsActive,
            fps = Time.FPS,
            cursor = Input.GetCursorState().ToString(),
            uiMode = UIManager.IsUIMode,
            openWindows = UIManager.OpenWindows,
            player = player == null ? null : DescribePlayer(player),
            target = player != null && AgentGame.TryRaycast(player, AgentGame.DefaultRayDistance, out var hit) ? AgentGame.DescribeHit(player, hit) : null,
            navigation = _navigation.Status
        };
    }

    private static object DescribePlayer(LocalAstronaut player)
    {
        var front = player.Front;
        return new
        {
            position = AgentGame.Vec(player.Position),
            front = AgentGame.Vec(front),
            up = AgentGame.Vec(player.Up),
            yaw = AgentGame.Round(AgentGame.Yaw(front)),
            pitch = AgentGame.Round(AgentGame.Pitch(front)),
            velocity = AgentGame.Vec(player.Gravity.IsActive ? player.Gravity.Velocity : player.InertiaController.Velocity),
            gameMode = player.GameMode.ToString(),
            alive = player.IsAlive,
            frozen = player.IsFrozen,
            canMove = player.CanMove,
            gravity = player.Gravity.IsActive,
            grounded = player.Gravity.IsGrounded,
            collision = player.CollisionEnabled,
            health = Stat(player.HealthBar?.StatsData),
            power = Stat(player.PowerBar?.StatsData),
            oxygen = Stat(player.OxygenBar?.StatsData),
            selectedSlot = PanelUI.SelectedSlotId,
            heldItem = AgentGame.DescribeItem(PanelUI.HeldItem, PanelUI.CurrentSlot()?.Count ?? 0)
        };
    }

    private static object Stat(StatsData stats)
    {
        return stats == null ? null : new { value = stats.Value, max = stats.MaxValue };
    }
}
