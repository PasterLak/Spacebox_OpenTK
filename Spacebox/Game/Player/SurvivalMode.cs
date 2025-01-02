namespace Spacebox.Game.Player;

public class SurvivalMode : GameModeBase
{
    private static InteractionHandler CreateInteractionHandler()
    {
        return new InteractionHandler(new InteractionDestroyBlock(), new HashSet<Type>()
        {
            typeof(InteractionDestroyBlock),
            typeof(InteractionDefault)
        });
    }

    public SurvivalMode(Astronaut player) : base(player, CreateInteractionHandler())
    {
        SetInertia(player.InertiaController);
    }
    
    public override GameMode GetGameMode()
    {
        return GameMode.Survival;
    }
    
    public override void OnEnable()
    {
        
    }

    public override void OnDisable()
    {
        InteractionHandler.Interaction.OnDisable();
    }

    public override void Update(Astronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
    }

    public override void HandleInput(Astronaut player)
    {
    }

    private void SetInertia(InertiaController inertiaController)
    {
        inertiaController.Enabled = true;
        inertiaController.SetParameters(
            walkTimeToMaxSpeed: 1f,
            walkTimeToStop: 0.5f,
            runTimeToMaxSpeed: 2f,
            runTimeToStop: 0.4f,
            walkMaxSpeed: 8,
            runMaxSpeed: 20
        );

        inertiaController.SetMode(isRunning: false);
        inertiaController.MaxSpeed = inertiaController.WalkMaxSpeed;

        inertiaController.InertiaType = InertiaType.Damping;
    }
}