using Engine.Networking;

namespace Spacebox.Game;

public interface IContainer
{
    string ContainerWindowId { get; }
    IReadOnlyList<Storage> GetContainerStorages();
    void WriteContainerState(NetWriter writer);
    void ReadContainerState(NetReader reader);
    void OnContainerChangedByNetwork();
}
