using Engine.Networking;
using SharpNBT;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class StorageComponent : BlockComponent, IContainerComponent, ISavedComponent
{
    public const string WindowId = "storage";
    public const int Priority = 20;

    public StorageComponent(StorageComponentDefinition settings)
    {
        Storage = new Storage(settings.Size) { Name = settings.StorageName };
        Storage.OnDataWasChanged += OnStorageChanged;
    }

    public Storage Storage { get; }

    public override int UsePriority => Priority;
    public string ContainerWindowId => WindowId;

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        StorageUI.OpenStorage(Storage, player);
    }

    public override void OnDestroyed(HitInfo info)
    {
        if (info.block == null) return;
        World.DropEffectManager.DropStorage(Storage, info.blockPositionWorld, 2, 0.1f, 2, 4);
    }

    public void CollectStorages(List<Storage> storages) => storages.Add(Storage);

    public void WriteContainerState(NetWriter writer)
    {
    }

    public void ReadContainerState(NetReader reader)
    {
    }

    public void OnContainerChangedByNetwork()
    {
    }

    public bool Save(CompoundTag root, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        if (!Storage.HasAnyItems()) return false;

        root.Add(new LongArrayTag(NBTKey.STORAGE.slots_data, StorageSlotsNbt.Write(Storage, itemsPalette, ref indexInPalette)));
        return true;
    }

    public void Load(CompoundTag tag, string[] paletteItems)
    {
        if (tag.TryGetValue<LongArrayTag>(NBTKey.STORAGE.slots_data, out var slotsData))
            StorageSlotsNbt.Read(Storage, slotsData.ToArray(), paletteItems);
    }

    public void OnLoaded()
    {
    }

    private void OnStorageChanged(Storage storage)
    {
        if (Host?.chunk != null) Host.chunk.IsModified = true;
    }
}
