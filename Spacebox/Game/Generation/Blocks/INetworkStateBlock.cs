using Engine.Networking;

namespace Spacebox.Game.Generation.Blocks;

public interface INetworkStateBlock
{
    void WriteNetworkState(NetWriter writer);
    void ReadNetworkState(NetReader reader);
}
