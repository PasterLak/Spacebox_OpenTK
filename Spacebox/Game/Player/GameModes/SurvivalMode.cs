namespace Spacebox.Game.Player.GameModes;
using Engine;
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

    public SurvivalMode(Astronaut player) : base(player, CreateInteractionHandler())
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

    public override void Update(Astronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
    }



    public override void HandleInput(Astronaut player)
    {
        base.HandleInput(player);
    }

}