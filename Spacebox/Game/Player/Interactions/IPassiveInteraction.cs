namespace Spacebox.Game.Player.Interactions;

public interface IPassiveInteraction
{
    void Update(LocalAstronaut player, InteractionMode activeInteraction);
}
