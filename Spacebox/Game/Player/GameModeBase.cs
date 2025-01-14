namespace Spacebox.Game.Player;

public abstract class GameModeBase
{
    protected Astronaut Player;
    public InteractionHandler InteractionHandler  { get; private set; }
    public abstract  GameMode GetGameMode();

    public GameModeBase(Astronaut player,  InteractionHandler interactionHandler)
    {
        Player = player;
        InteractionHandler  = interactionHandler;
    }

    public void SetInteraction(InteractionMode iteration)
    {
      
        if (InteractionHandler != null)
        {
            InteractionHandler.SetInteraction(iteration, GetGameMode());
        }
    }
    
    public abstract void OnEnable();
    public abstract void OnDisable();
    public abstract void Update(Astronaut player);
    public abstract void UpdateInteraction(Astronaut player);
    public abstract void HandleInput(Astronaut player);
}



