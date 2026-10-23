using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class DisassemblerActivity : IMachineActivity
{
    public bool Handles(Block block) => block is DisassemblerBlock;

    public bool IsWorking(Block block) => block is DisassemblerBlock disassembler && disassembler.HasPower && disassembler.RequiredTicks > 0;

    public IDisposable? WatchFinished(Block block, Action finished)
    {
        return block is DisassemblerBlock disassembler ? new Subscription(disassembler, finished) : null;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly DisassemblerBlock _disassembler;
        private readonly Action<DisassemblerBlock> _handler;

        public Subscription(DisassemblerBlock disassembler, Action finished)
        {
            _disassembler = disassembler;
            _handler = _ => finished();
            _disassembler.OnDisassembled += _handler;
        }

        public void Dispose()
        {
            _disassembler.OnDisassembled -= _handler;
        }
    }
}
