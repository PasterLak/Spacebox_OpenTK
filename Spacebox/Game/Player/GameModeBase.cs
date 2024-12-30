namespace Spacebox.Game.Player;

public abstract class GameModeBase
{
    protected Astronaut Player;
    protected InteractionHandler InteractionHandler;

    public GameModeBase(Astronaut player,  InteractionHandler interactionHandler)
    {
        Player = player;
        InteractionHandler  = interactionHandler;
    }

    public void SetInteraction(InteractionMode iteration)
    {
        if (InteractionHandler != null)
        {
            InteractionHandler.SetInteraction(iteration);
        }
    }
    
    public abstract void Update(Astronaut player);
    public abstract void HandleInput(Astronaut player);
}