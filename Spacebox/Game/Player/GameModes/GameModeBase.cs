namespace Spacebox.Game.Player.GameModes;
using Engine;
using Spacebox.Game.Player.Interactions;

public abstract class GameModeBase
{
    protected LocalAstronaut Player;
    public InteractionHandler InteractionHandler  { get; private set; }
    public abstract  GameMode GetGameMode();

    public GameModeBase(LocalAstronaut player,  InteractionHandler interactionHandler)
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
    public abstract void Update(LocalAstronaut player);
    public virtual void Render(LocalAstronaut player)
    {
        InteractionHandler.Render(player);
    }
    public abstract void UpdateInteraction(LocalAstronaut player);
    public abstract void HandleInput(LocalAstronaut player);
}



