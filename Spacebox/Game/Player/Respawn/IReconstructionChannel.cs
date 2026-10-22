using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public interface IReconstructionChannel
{
    string GetBlocker(Vector3 position);
    void Request(Vector3 position, Action<ReconstructionGrant?> done);
}
