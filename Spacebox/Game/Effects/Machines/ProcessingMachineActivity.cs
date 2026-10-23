using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class ProcessingMachineActivity : IMachineActivity
{
    public bool Handles(Block block) => block is ResourceProcessingBlock;

    public bool IsWorking(Block block) => block is ResourceProcessingBlock machine && machine.IsRunning && machine.HasPower;

    public IDisposable? WatchFinished(Block block, Action finished)
    {
        return block is ResourceProcessingBlock machine ? new Subscription(machine, finished) : null;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly ResourceProcessingBlock _machine;
        private readonly Action<ResourceProcessingBlock> _handler;

        public Subscription(ResourceProcessingBlock machine, Action finished)
        {
            _machine = machine;
            _handler = _ => finished();
            _machine.OnCrafted += _handler;
        }

        public void Dispose()
        {
            _machine.OnCrafted -= _handler;
        }
    }
}
