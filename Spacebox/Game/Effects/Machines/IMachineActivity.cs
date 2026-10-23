using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public interface IMachineActivity
{
    bool Handles(Block block);
    bool IsWorking(Block block);
    IDisposable? WatchFinished(Block block, Action finished);
}
