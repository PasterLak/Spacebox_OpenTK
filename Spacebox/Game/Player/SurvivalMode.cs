namespace Spacebox.Game.Player;

public class SurvivalMode : MovementMode
{
    private static InteractionHandler CreateInteractionHandler()
    {
        return new InteractionHandler(new InteractionDestroyBlock(), new HashSet<Type>()
        {
           typeof(InteractionDestroyBlock),
            typeof(InteractionPlaceBlock),
            typeof(InteractionConsumeItem),
            typeof(InteractionDefault)
        }, GameMode.Survival);
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