using BenchmarkDotNet.Attributes;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Resource;

namespace Spacebox.Benchmarks;

[MemoryDiagnoser]
public class BlockComponentBenchmarks
{
    private const int TickCount = 1000;

    private BlockData _plainData;
    private BlockData _consumerData;
    private BlockData _lampData;
    private BlockData _furnaceData;
    private BlockData _generatorData;
    private BlockComponentDefinition[] _lampComponents;
    private BlockComponentDefinition[] _furnaceComponents;
    private BlockComponentDefinition[] _generatorComponents;
    private ConsumerBlock _consumerClass;
    private ComponentBlock _lampComponent;
    private ComponentBlock _generatorComponent;

    [GlobalSetup]
    public void Setup()
    {
        _plainData = new BlockData("Stone", "block") { Id = 10 };
        _consumerData = new BlockData("Lamp", "consumer", false, new OpenTK.Mathematics.Vector3(1f, 1f, 0.5f)) { Id = 11 };
        _lampData = new BlockData("Lamp", BlockComponentRegistry.ModularType, false, new OpenTK.Mathematics.Vector3(1f, 1f, 0.5f)) { Id = 12 };
        _furnaceData = new BlockData("Furnace", "furnace") { Id = 13 };
        _generatorData = new GeneratorBlockData(new BlockData("Generator", "generator") { Id = 14 }, new PowerGeneratorJSON { Power = 50 });

        _lampComponents = new BlockComponentDefinition[] { new PowerConsumerComponentDefinition(20), PoweredLightComponentDefinition.Instance };
        _furnaceComponents = BlockComponentRegistry.Resolve(_furnaceData, null);
        _generatorComponents = BlockComponentRegistry.Resolve(_generatorData, null);

        _consumerClass = new ConsumerBlock(_consumerData);
        _lampComponent = ComponentBlocks.Create(_lampData, _lampComponents);
        _generatorComponent = ComponentBlocks.Create(_generatorData, _generatorComponents);
    }

    [Benchmark(Baseline = true)]
    public Block Create_PlainBlock() => new Block(_plainData);

    [Benchmark]
    public Block Create_ConsumerClass() => new ConsumerBlock(_consumerData);

    [Benchmark]
    public Block Create_ConsumerLightComponents() => ComponentBlocks.Create(_lampData, _lampComponents);

    [Benchmark]
    public Block Create_GeneratorComponent() => ComponentBlocks.Create(_generatorData, _generatorComponents);

    [Benchmark]
    public Block Create_FurnaceComponent() => ComponentBlocks.Create(_furnaceData, _furnaceComponents);

    [Benchmark]
    public short Tick_ConsumerClass()
    {
        for (int i = 0; i < TickCount; i++) _consumerClass.TickElectric();
        return _consumerClass.CurrentPower;
    }

    [Benchmark]
    public short Tick_ConsumerLightComponents()
    {
        for (int i = 0; i < TickCount; i++) _lampComponent.TickElectric();
        return _lampComponent.CurrentPower;
    }

    [Benchmark]
    public short Tick_GeneratorComponent()
    {
        for (int i = 0; i < TickCount; i++) _generatorComponent.TickElectric();
        return _generatorComponent.CurrentPower;
    }
}
