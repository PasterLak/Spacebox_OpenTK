using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game.Effects.Machines;

public sealed class ProcessingMachineActivity : IMachineActivity
{
    public bool Handles(Block block) => Processing(block) != null;

    public bool IsWorking(Block block) => Processing(block) is { } machine && machine.IsRunning && machine.HasPower;

    public IDisposable? WatchFinished(Block block, Action finished)
    {
        return Processing(block) is { } machine ? new Subscription(machine, finished) : null;
    }

    private static ProcessingComponent Processing(Block block) => block is ComponentBlock host && host.TryGet(out ProcessingComponent processing) ? processing : null;

    private sealed class Subscription : IDisposable
    {
        private readonly ProcessingComponent _machine;
        private readonly Action<ProcessingComponent> _handler;

        public Subscription(ProcessingComponent machine, Action finished)
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
