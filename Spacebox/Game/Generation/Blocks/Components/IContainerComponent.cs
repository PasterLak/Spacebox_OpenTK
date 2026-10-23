using Engine.Networking;

namespace Spacebox.Game.Generation.Blocks.Components;

public interface IContainerComponent
{
    string ContainerWindowId { get; }
    void CollectStorages(List<Storage> storages);
    void WriteContainerState(NetWriter writer);
    void ReadContainerState(NetReader reader);
    void OnContainerChangedByNetwork();
}
