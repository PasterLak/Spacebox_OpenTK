using Engine;
using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Logistics;
using Spacebox.Game.Resource;

namespace Spacebox.Tests.Game.BlockComponents;

[Collection(GameAssetsCollection.Name)]
public class ComponentBlockTests
{
    [Fact]
    public void Furnace_IsAStatefulContainerWithTheOldStorageLayout()
    {
        var block = ComponentBlocks.Create(ComponentTestAssets.Data("furnace"));

        var container = Assert.IsAssignableFrom<IContainerBlock>(block);
        Assert.IsAssignableFrom<INetworkStateBlock>(block);
        Assert.IsAssignableFrom<IEntityBlockListener>(block);
        Assert.Equal(ProcessingComponent.WindowId, container.ContainerWindowId);
        Assert.Equal(new[] { "InputStorage", "OutputStorage", "FuelStorage" }, container.GetContainerStorages().Select(s => s.Name));
        Assert.Same(container.GetContainerStorages(), container.GetContainerStorages());
        Assert.Equal(ElectricalFlags.None, ((ComponentBlock)block).EFlags);
    }

    [Fact]
    public void Generator_IsAPlainComponentBlockWithGeneratorPower()
    {
        var data = new GeneratorBlockData(ComponentTestAssets.Data("generator"), new PowerGeneratorJSON { Power = 120 }, new ExplosiveJSON { Radius = 3 });

        var block = Assert.IsType<ComponentBlock>(ComponentBlocks.Create(data));

        Assert.Equal(ElectricalFlags.CanGenerate | ElectricalFlags.CanTransfer, block.EFlags);
        Assert.Equal(120, block.GenerationRate);
        Assert.Equal(1200, block.MaxPower);
        Assert.NotNull(block.Explosion);
        Assert.True(block.UsesNetworkPower);
        Assert.False(block.ConsumesPower);
    }

    [Fact]
    public void PoweredProcessing_ConsumesAndWaitsForPower()
    {
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing(power: 30));
        Assert.True(block.TryGet<ProcessingComponent>(out var processing));

        Assert.Equal(ElectricalFlags.CanConsume, block.EFlags);
        Assert.Equal(30, block.ConsumptionRate);
        Assert.True(processing.RequiresPower);
        Assert.False(processing.HasPower);

        block.IsActive = true;

        Assert.True(processing.HasPower);
    }

    [Fact]
    public void ProcessingAndConsumer_SumTheirPowerDemand()
    {
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing(power: 30), new PowerConsumerComponentDefinition(15));

        Assert.Equal(45, block.ConsumptionRate);
        Assert.True(block.TryGet<ProcessingComponent>(out var processing));
        Assert.True(processing.RequiresPower);
    }

    [Fact]
    public void ProcessingPlusStorage_ExposesAllStoragesAndOpensTheMachineWindow()
    {
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing(), new StorageComponentDefinition(new Vector2Byte(3, 2), "Smelter"));

        var container = Assert.IsAssignableFrom<IContainerBlock>(block);
        Assert.Equal(4, container.GetContainerStorages().Count);
        Assert.Equal(ProcessingComponent.WindowId, container.ContainerWindowId);
        Assert.True(block.TryGet<ProcessingComponent>(out var processing));
        Assert.NotNull(processing.BufferStorage);
        Assert.Equal(6, processing.BufferStorage.SlotsCount);
    }

    [Fact]
    public void StorageOnly_IsAContainerWithoutNetworkState()
    {
        var block = ComponentTestAssets.Modular(new StorageComponentDefinition(new Vector2Byte(2, 2), "Locker"));

        var container = Assert.IsAssignableFrom<IContainerBlock>(block);
        Assert.False(block is INetworkStateBlock);
        Assert.Equal(StorageComponent.WindowId, container.ContainerWindowId);
        Assert.Equal("Locker", container.GetContainerStorages()[0].Name);
    }

    [Fact]
    public void LightAndConsumer_GlowOnlyWhilePowered()
    {
        var data = new BlockData("Lamp", BlockComponentRegistry.ModularType, false, new OpenTK.Mathematics.Vector3(1f, 0.5f, 0f)) { Id_string = "comptest:lamp" };
        var block = ComponentBlocks.Create(data, new BlockComponentDefinition[] { new PowerConsumerComponentDefinition(20), PoweredLightComponentDefinition.Instance });

        Assert.False(block is IContainerBlock);
        Assert.False(block is INetworkStateBlock);
        Assert.Equal(ElectricalFlags.CanConsume, block.EFlags);
        Assert.False(block.IsLight);
        Assert.Equal(0f, block.LightLevel);

        block.IsActive = true;

        Assert.True(block.IsLight);
        Assert.Equal(PoweredLightComponent.SourceLevel, block.LightLevel);
        Assert.Equal(new Color3Byte(data.LightColor), block.LightColor);

        block.IsActive = false;

        Assert.False(block.IsLight);
        Assert.Equal(0f, block.LightLevel);
    }

    [Fact]
    public void TryGet_FindsOnlyAttachedComponents()
    {
        var lamp = ComponentTestAssets.Modular(new PowerConsumerComponentDefinition(20));
        var storage = ComponentTestAssets.Modular(new StorageComponentDefinition(new Vector2Byte(1, 1), "Box"));

        Assert.False(lamp.TryGet<StorageComponent>(out _));
        Assert.True(storage.TryGet<StorageComponent>(out _));
    }

    [Fact]
    public void ProcessingNetworkState_RoundTripsWithTheOldLayout()
    {
        using var assets = new ComponentTestAssets();
        var server = (StatefulContainerComponentBlock)ComponentBlocks.Create(ComponentTestAssets.Data("furnace"), new BlockComponentDefinition[] { ComponentTestAssets.Processing() });
        server.TryGet<ProcessingComponent>(out var serverProcessing);
        serverProcessing.InputStorage.SetSlot(0, 0, assets.Ore, 3);
        Assert.True(serverProcessing.TryStartTask(out _));
        serverProcessing.CurrentTick = 2;

        var writer = new NetWriter(16);
        server.WriteNetworkState(writer);
        Assert.Equal(1 + 2 + 2 + 1, writer.Length);

        var client = (StatefulContainerComponentBlock)ComponentBlocks.Create(ComponentTestAssets.Data("furnace"), new BlockComponentDefinition[] { ComponentTestAssets.Processing() });
        client.ReadNetworkState(new NetReader(writer.Span.ToArray()));
        client.TryGet<ProcessingComponent>(out var clientProcessing);

        Assert.True(clientProcessing.IsRunning);
        Assert.Equal(2, clientProcessing.CurrentTick);
        Assert.Equal(serverProcessing.CraftTicks, clientProcessing.CraftTicks);
        serverProcessing.StopTask();
    }

    [Fact]
    public void Processing_CraftsThroughItsTickTask()
    {
        using var assets = new ComponentTestAssets(recipeTicks: 3);
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing());
        block.TryGet<ProcessingComponent>(out var processing);
        processing.InputStorage.SetSlot(0, 0, assets.Ore, 2);

        Assert.True(processing.TryStartTask(out var task));
        Assert.True(block.IsActive);
        for (int i = 0; i < 6; i++) task.Update();

        Assert.False(processing.HasInput());
        Assert.Equal(2, processing.OutputStorage.GetSlot(0, 0).Count);
        Assert.Equal(assets.Ingot.Id, processing.OutputStorage.GetSlot(0, 0).Item.Id);
        Assert.False(processing.IsRunning);
        Assert.False(block.IsActive);
    }

    [Fact]
    public void ProcessingWithBuffer_FeedsInputAndStoresProducts()
    {
        using var assets = new ComponentTestAssets(recipeTicks: 2);
        var block = ComponentTestAssets.Modular(ComponentTestAssets.Processing(), new StorageComponentDefinition(new Vector2Byte(3, 1), "Smelter"));
        block.TryGet<ProcessingComponent>(out var processing);
        var buffer = processing.BufferStorage;

        buffer.SetSlot(0, 0, assets.Rock, 5);
        buffer.SetSlot(1, 0, assets.Ore, 3);

        Assert.Equal(3, processing.InputStorage.GetSlot(0, 0).Count);
        Assert.Equal(5, buffer.GetSlot(0, 0).Count);
        Assert.True(processing.IsRunning);

        var task = processing.Task;
        for (int i = 0; i < 6; i++) task.Update();

        Assert.False(processing.HasInput());
        Assert.False(processing.HasOutput());
        Assert.Equal(3, buffer.GetTotalCountOf(assets.Ingot));
        Assert.Equal(5, buffer.GetTotalCountOf(assets.Rock));
    }

    [Fact]
    public void MachineEndpoint_ExtractsProductsFromTheBufferButNotQueuedInput()
    {
        using var assets = new ComponentTestAssets();
        var block = ComponentTestAssets.Modular(new StorageComponentDefinition(new Vector2Byte(2, 1), "Smelter"), ComponentTestAssets.Processing(power: 10));
        block.TryGet<ProcessingComponent>(out var processing);
        processing.BufferStorage.SetSlot(0, 0, assets.Ore, 4);
        processing.BufferStorage.SetSlot(1, 0, assets.Ingot, 2);

        Assert.True(ItemTransferEndpoints.TryGet(block, out var endpoint));
        var slot = endpoint.FindExtractable(_ => true);

        Assert.NotNull(slot);
        Assert.Equal(assets.Ingot.Id, slot.Item.Id);
    }
}
