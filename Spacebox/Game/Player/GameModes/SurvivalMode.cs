namespace Spacebox.Game.Player.GameModes;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Player.Interactions;

public class SurvivalMode : MovementMode
{
    private static InteractionHandler CreateInteractionHandler()
    {
        return new InteractionHandler(new InteractionDefault(), new HashSet<Type>()
        {
           typeof(InteractionDestroyBlockSurvival),
            typeof(InteractionPlaceBlock),
            typeof(InteractionConsumeItem),
            typeof(InteractionShoot),
            typeof(InteractionDefault),
             typeof(InteractionEraser)
        },
        GameMode.Survival);
    }

    public SurvivalMode(LocalAstronaut player) : base(player, CreateInteractionHandler())
    {

    }

    public override GameMode GetGameMode()
    {
        return GameMode.Survival;
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();

    }

    public override void Update(LocalAstronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
        player.OxygenBar.Update(World.Instance != null && World.Instance.IsBreathable(player.Position));
        UpdateSuffocation(player);
    }

    private void UpdateSuffocation(LocalAstronaut player)
    {
        if (!player.IsAlive || !player.OxygenBar.StatsData.IsMinReached) return;

        player.TakeDamage(player.HealthBar.StatsData.Value, new DeathCase("Ran out of oxygen"));
    }



    public override void HandleInput(LocalAstronaut player)
    {
        base.HandleInput(player);
    }

}