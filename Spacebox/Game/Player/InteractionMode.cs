namespace Spacebox.Game.Player;
using Engine;
public abstract class InteractionMode
{
    public GameMode GameMode { get; set; } = GameMode.Spectator;

    public bool AllowReload = false;
    public abstract void OnEnable();
    public abstract void OnDisable();
    public abstract void Update(Astronaut player);
    public virtual void Render(Astronaut player)
    {

    }


}



