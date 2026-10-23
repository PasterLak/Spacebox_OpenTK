using Engine.Networking;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public class StatefulComponentBlock : ComponentBlock, INetworkStateBlock
{
    private readonly ComponentStateChannel _state;

    public StatefulComponentBlock(BlockData blockData, BlockComponentDefinition[] definitions) : base(blockData, definitions)
    {
        _state = new ComponentStateChannel(this);
    }

    public override void MarkStateDirty() => Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, Cell, this);

    public void WriteNetworkState(NetWriter writer) => _state.Write(writer);

    public void ReadNetworkState(NetReader reader) => _state.Read(reader);
}
