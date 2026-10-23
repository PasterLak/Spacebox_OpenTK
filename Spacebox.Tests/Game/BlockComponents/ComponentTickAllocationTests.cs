using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game.BlockComponents;

[Collection(GameAssetsCollection.Name)]
public class ComponentTickAllocationTests
{
    private const int Ticks = 240;

    private static ComponentBlock Generator() =>
        ComponentBlocks.Create(new GeneratorBlockData(ComponentTestAssets.Data("generator"), new PowerGeneratorJSON { Power = 80 }));

    private static ProcessingComponent RunningMachine(ComponentTestAssets assets, out Engine.TickTask task)
    {
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing(), PoweredLightComponentDefinition.Instance);
        block.TryGet<ProcessingComponent>(out var processing);
        processing.InputStorage.SetSlot(0, 0, assets.Ore, 64);
        Assert.True(processing.TryStartTask(out var processTask));
        task = processTask;
        return processing;
    }

    [Fact]
    public void ElectricTickDispatch_DoesNotAllocate()
    {
        var warm = Generator();
        for (int i = 0; i < Ticks; i++) warm.TickElectric();
        var block = Generator();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ticks; i++) block.TickElectric();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void ProcessingTicksAndCrafts_DoNotAllocate()
    {
        using var assets = new ComponentTestAssets(recipeTicks: 4);
        var warm = RunningMachine(assets, out var warmTask);
        for (int i = 0; i < Ticks; i++) warmTask.Update();
        var processing = RunningMachine(assets, out var task);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ticks; i++) task.Update();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(Ticks / 4, processing.OutputStorage.GetSlot(0, 0).Count);
        warm.StopTask();
        processing.StopTask();
    }
}
