using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public sealed class LocalReconstructionChannel : IReconstructionChannel
{
    public string GetBlocker(Vector3 position) => DnaReconstructorRegistry.GetBlocker();

    public void Request(Vector3 position, Action<ReconstructionGrant?> done)
    {
        DnaReconstructionService.Request(position, NetworkManager.Instance.LocalPeerId, done);
    }
}
