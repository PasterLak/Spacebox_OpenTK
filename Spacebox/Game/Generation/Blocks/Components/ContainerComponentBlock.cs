using Engine.Networking;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public class ContainerComponentBlock : ComponentBlock, IContainerBlock
{
    private readonly IContainerComponent[] _containers;
    private readonly Storage[] _storages;

    public ContainerComponentBlock(BlockData blockData, BlockComponentDefinition[] definitions) : base(blockData, definitions)
    {
        var containers = new List<IContainerComponent>();
        var storages = new List<Storage>();
        for (int i = 0; i < ComponentCount; i++)
        {
            if (GetComponent(i) is not IContainerComponent container) continue;
            containers.Add(container);
            container.CollectStorages(storages);
        }

        _containers = containers.ToArray();
        _storages = storages.ToArray();
        ContainerWindowId = UseComponent is IContainerComponent primary ? primary.ContainerWindowId : _containers.Length > 0 ? _containers[0].ContainerWindowId : "storage";
    }

    public string ContainerWindowId { get; }

    public IReadOnlyList<Storage> GetContainerStorages() => _storages;

    public void WriteContainerState(NetWriter writer)
    {
        for (int i = 0; i < _containers.Length; i++)
            _containers[i].WriteContainerState(writer);
    }

    public void ReadContainerState(NetReader reader)
    {
        for (int i = 0; i < _containers.Length; i++)
            _containers[i].ReadContainerState(reader);
    }

    public void OnContainerChangedByNetwork()
    {
        for (int i = 0; i < _containers.Length; i++)
            _containers[i].OnContainerChangedByNetwork();
    }
}
