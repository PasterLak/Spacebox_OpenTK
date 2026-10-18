using Engine.Networking;

namespace Spacebox.Game.Generation.Blocks;

public interface IContainerBlock
{
    string ContainerWindowId { get; }
    IReadOnlyList<Storage> GetContainerStorages();
    void WriteContainerState(NetWriter writer);
    void ReadContainerState(NetReader reader);
    void OnContainerChangedByNetwork();
}
