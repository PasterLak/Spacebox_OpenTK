namespace Spacebox.Game.Player.Respawn;

public interface IReconstructionEffects
{
    void Begin();
    void Update(float progress, int step);
    void End();
}
