namespace Spacebox.Game.Mobs;

public interface IMobBrain
{
    string StateName { get; }
    void Update(float deltaTime);
}
