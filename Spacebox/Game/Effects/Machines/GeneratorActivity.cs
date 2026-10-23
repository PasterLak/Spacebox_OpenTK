using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;

namespace Spacebox.Game.Effects.Machines;

public sealed class GeneratorActivity : IMachineActivity
{
    public bool Handles(Block block) => block is ComponentBlock host && host.TryGet(out PowerGeneratorComponent _);

    public bool IsWorking(Block block) => Handles(block) && block.EnableEmission;

    public IDisposable? WatchFinished(Block block, Action finished) => null;
}
