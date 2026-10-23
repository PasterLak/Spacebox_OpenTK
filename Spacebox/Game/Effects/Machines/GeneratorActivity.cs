using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class GeneratorActivity : IMachineActivity
{
    public bool Handles(Block block) => block is GeneratorBlock;

    public bool IsWorking(Block block) => block is GeneratorBlock generator && generator.EnableEmission;

    public IDisposable? WatchFinished(Block block, Action finished) => null;
}
