using Engine.Networking;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public class StatefulContainerComponentBlock : ContainerComponentBlock, INetworkStateBlock
{
    private readonly ComponentStateChannel _state;

    public StatefulContainerComponentBlock(BlockData blockData, BlockComponentDefinition[] definitions) : base(blockData, definitions)
    {
        _state = new ComponentStateChannel(this);
    }

    public override void MarkStateDirty() => Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, Cell, this);

    public void WriteNetworkState(NetWriter writer) => _state.Write(writer);

    public void ReadNetworkState(NetReader reader) => _state.Read(reader);
}
