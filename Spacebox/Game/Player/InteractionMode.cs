namespace Spacebox.Game.Player;

public abstract class InteractionMode
{
    public GameMode GameMode { get; set; } = GameMode.Spectator;

    public abstract void OnEnable();
    public abstract void OnDisable();
    public abstract void Update(Astronaut player);
}