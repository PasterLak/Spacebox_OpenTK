namespace Spacebox.Game.Player;

public abstract class InteractionMode
{
    public abstract void OnEnable();
    public abstract void OnDisable();
    public abstract void Update(Astronaut player);
}