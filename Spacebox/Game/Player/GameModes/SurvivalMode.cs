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
            typeof(InteractionSpawnMob),
            typeof(InteractionEquipItem),
            typeof(InteractionShoot),
            typeof(InteractionDefault),
            typeof(InteractionRepair),
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

    private readonly SuffocationDamage _suffocation = new SuffocationDamage();

    private void UpdateSuffocation(LocalAstronaut player)
    {
        int damage = _suffocation.Update(Time.Delta, player.IsAlive && player.OxygenBar.StatsData.IsMinReached);
        if (damage > 0) player.TakeDamage(damage, new DeathCase("Ran out of oxygen"));
    }



    public override void HandleInput(LocalAstronaut player)
    {
        base.HandleInput(player);
    }

}