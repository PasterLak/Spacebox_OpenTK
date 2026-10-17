namespace Spacebox.Game.Player.Interactions;
using Engine;
using Spacebox.Game.Player.GameModes;

public abstract class InteractionMode
{
    public GameMode GameMode { get; set; } = GameMode.Spectator;

    public bool AllowReload = false;
    public abstract void OnEnable();
    public abstract void OnDisable();
    public abstract void Update(LocalAstronaut player);
    public virtual void Render(LocalAstronaut player)
    {

    }


}



