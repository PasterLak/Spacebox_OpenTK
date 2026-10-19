using Spacebox.Game.GUI;

namespace Spacebox.Game.Player;

public static class ConsumableUse
{
    public static void Apply(LocalAstronaut player, ConsumableItem consumable)
    {
        if (player == null || consumable == null) return;

        if (consumable.HealAmount > 0) ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 1, 0), 0.2f);
        if (consumable.PowerAmount > 0) ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 0, 1), 0.15f);
        if (consumable.OxygenAmount > 0) ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 1, 1), 0.15f);

        ItemUseCue.PlayEffects(player.Effects, consumable);
        player.NotifyItemUsed(consumable);

        player.HealthBar.StatsData.Increment(consumable.HealAmount);
        player.PowerBar.StatsData.Increment(consumable.PowerAmount);
        player.OxygenBar.StatsData.Increment(consumable.OxygenAmount);
    }
}
