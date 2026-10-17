namespace Spacebox.Game.Player.GameModes;
using Engine;
using Spacebox.Game.Player.Interactions;

public class SurvivalMode : MovementMode
{
    private const float SuffocationInterval = 1f;
    private const int SuffocationDamage = 5;
    private float _suffocationTime;

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
        player.OxygenBar.Update();
        UpdateSuffocation(player);
    }

    private void UpdateSuffocation(LocalAstronaut player)
    {
        if (!player.IsAlive || !player.OxygenBar.StatsData.IsMinReached)
        {
            _suffocationTime = 0f;
            return;
        }

        _suffocationTime += Time.Delta;
        if (_suffocationTime < SuffocationInterval) return;

        _suffocationTime -= SuffocationInterval;
        player.TakeDamage(SuffocationDamage, new DeathCase("Ran out of oxygen"));
    }



    public override void HandleInput(LocalAstronaut player)
    {
        base.HandleInput(player);
    }

}