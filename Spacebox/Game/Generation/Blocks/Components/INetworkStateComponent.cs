using Engine.Networking;

namespace Spacebox.Game.Generation.Blocks.Components;

public interface INetworkStateComponent
{
    void WriteNetworkState(NetWriter writer);
    void ReadNetworkState(NetReader reader);
}
