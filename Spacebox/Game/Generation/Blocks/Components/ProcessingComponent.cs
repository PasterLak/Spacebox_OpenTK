using Engine;
using Engine.Networking;
using SharpNBT;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class ProcessingComponent : BlockComponent, IContainerComponent, INetworkStateComponent, ISavedComponent
{
    public const string WindowId = "resourceProcessing";
    public const int Priority = 30;

    private const float TestingCoefficient = 1f;
    private const string InputKey = "input_data";
    private const string OutputKey = "output_data";
    private const string FuelKey = "fuel_data";
    private const string TickKey = "current_tick";

    private readonly ProcessingComponentDefinition _settings;
    private Recipe _recipe;
    private StorageComponent _buffer;
    private bool _isRunning;
    private bool _syncedPower;
    private byte _craftedCount;
    private int _syncedCraftedCount = -1;
    private bool _exchanging;
    private short _loadedTicks;

    public ProcessingComponent(ProcessingComponentDefinition settings)
    {
        _settings = settings;

        InputStorage.Name = "InputStorage";
        FuelStorage.Name = "FuelStorage";
        OutputStorage.Name = "OutputStorage";
        InputStorage.GetSlot(0, 0).Name = "Input";
        FuelStorage.GetSlot(0, 0).Name = "Fuel";
        OutputStorage.GetSlot(0, 0).Name = "Output";

        InputStorage.OnDataWasChanged += OnAnySlotWasChanged;
        FuelStorage.OnDataWasChanged += OnAnySlotWasChanged;
        OutputStorage.OnDataWasChanged += OnAnySlotWasChanged;
    }

    public Storage InputStorage { get; } = new Storage(1, 1);
    public Storage OutputStorage { get; } = new Storage(1, 1);
    public Storage FuelStorage { get; } = new Storage(1, 1);
    public Storage BufferStorage => _buffer?.Storage;

    public string RecipeType => _settings.RecipeType;
    public ushort Power => _settings.Power;
    public float Efficiency => _settings.Efficiency;
    public int ProcessedItems { get; set; }
    public Action<ProcessingComponent> OnCrafted;
    public TickTask Task { get; private set; }
    public short CraftTicks { get; set; }
    public short CurrentTick { get; set; }

    public override int UsePriority => Priority;
    public override bool ConsumesPower => Power > 0;
    public string ContainerWindowId => WindowId;

    public bool RequiresPower => Host.ConsumesPower;
    public bool HasPower => !RequiresPower || (Spacebox.Network.GameSession.IsRemoteClient ? _syncedPower : Host.IsActive);
    public bool IsEntityDisposed => Host.IsEntityDisposed;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            bool changed = _isRunning != value;
            _isRunning = value;
            Host.SetEmission(_isRunning && HasPower, true);
            if (!Host.UsesNetworkPower) Host.IsActive = _isRunning;
            if (changed) Host.MarkStateDirty();
        }
    }

    protected override void OnAttached()
    {
        if (Power > 0)
        {
            Host.EFlags |= ElectricalFlags.CanConsume;
            Host.ConsumptionRate = (ushort)Math.Min(Host.ConsumptionRate + Power, ushort.MaxValue);
        }

        //LightLevel

        Host.SetEmission(false, false);

        if (Host.TryGet(out _buffer))
            _buffer.Storage.OnDataWasChanged += OnBufferChanged;
    }

    public override void OnActiveStateChanged(bool isActive)
    {
        if (!RequiresPower) return;
        Host.SetEmission(_isRunning && isActive, true);
        if (Spacebox.Network.GameSession.IsRemoteClient) return;
        if (Task != null)
        {
            if (isActive) Task.Continue();
            else Task.Pause();
        }
        else if (isActive) TryStart();
        Host.MarkStateDirty();
    }

    public override void OnRemovedFromEntity()
    {
        StopTask();
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        var processing = GameUi.Current?.Processing;
        if (processing == null) return;
        processing.Toggle(player);
        processing.Activate(this, player);
    }

    public override void OnDestroyed(HitInfo info)
    {
        var storageList = new List<Storage> { InputStorage, FuelStorage, OutputStorage };
        var combinedStorage = Storage.CombineStorages(storageList, 5, 5);

        InputStorage.Clear();
        FuelStorage.Clear();
        OutputStorage.Clear();

        World.DropEffectManager.DropStorage(combinedStorage, info.blockPositionWorld);
    }

    public void CollectStorages(List<Storage> storages)
    {
        storages.Add(InputStorage);
        storages.Add(OutputStorage);
        storages.Add(FuelStorage);
    }

    public void WriteContainerState(NetWriter writer)
    {
        writer.Write(IsRunning);
        writer.Write(CurrentTick);
        writer.Write(CraftTicks);
        writer.Write(HasPower);
        writer.Write(_craftedCount);
    }

    public void ReadContainerState(NetReader reader)
    {
        bool running = reader.ReadBool();
        CurrentTick = reader.ReadShort();
        CraftTicks = reader.ReadShort();
        _syncedPower = reader.ReadBool();
        byte crafted = reader.ReadByte();
        bool finished = _syncedCraftedCount >= 0 && crafted != _syncedCraftedCount;
        _syncedCraftedCount = crafted;
        if (running != IsRunning) IsRunning = running;
        else Host.SetEmission(_isRunning && HasPower, true);
        if (finished) OnCrafted?.Invoke(this);
    }

    public void WriteNetworkState(NetWriter writer) => WriteContainerState(writer);

    public void ReadNetworkState(NetReader reader) => ReadContainerState(reader);

    public void OnContainerChangedByNetwork()
    {
        if (!IsRunning) TryStart();
    }

    public bool Save(CompoundTag root, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var inSlot = InputStorage.GetSlot(0, 0);
        var outSlot = OutputStorage.GetSlot(0, 0);
        var fuelSlot = FuelStorage.GetSlot(0, 0);

        if (!inSlot.HasItem && !outSlot.HasItem && !fuelSlot.HasItem && CurrentTick == 0) return false;

        WriteSlot(root, InputKey, inSlot, itemsPalette, ref indexInPalette);
        WriteSlot(root, OutputKey, outSlot, itemsPalette, ref indexInPalette);
        WriteSlot(root, FuelKey, fuelSlot, itemsPalette, ref indexInPalette);
        root.Add(new ShortTag(TickKey, CurrentTick));
        return true;
    }

    public void Load(CompoundTag tag, string[] paletteItems)
    {
        ReadSlot(tag, InputKey, InputStorage, paletteItems);
        ReadSlot(tag, OutputKey, OutputStorage, paletteItems);
        ReadSlot(tag, FuelKey, FuelStorage, paletteItems);
        _loadedTicks = tag.TryGetValue<ShortTag>(TickKey, out var tickTag) ? tickTag.Value : (short)0;
    }

    public void OnLoaded()
    {
        if (_loadedTicks > 0) RestoreTaskProgress(_loadedTicks);
        else TryStart();
        _loadedTicks = 0;
    }

    public void SetStorageAfterLoadFromNBT(Item item, byte count, Storage storage)
    {
        storage.SetSlot(0, 0, item, count);
    }

    public int GetTaskProgress()
    {
        if (CraftTicks == 0 || CurrentTick == 0) return 0;

        return (int)(CurrentTick / (float)CraftTicks * 100f);
    }

    public int GetTicksWithEfficiency(int basicTicks)
    {
        return Math.Max(1, (int)(basicTicks * TestingCoefficient / Efficiency));
    }

    public bool HasInput() => InputStorage.GetSlot(0, 0).Count > 0;
    public bool HasOutput() => OutputStorage.GetSlot(0, 0).Count > 0;
    public bool HasFuel() => FuelStorage.GetSlot(0, 0).Count > 0;

    public void TryStart()
    {
        if (!InputStorage.GetSlot(0, 0).HasItem) return;
        if (TryStartTask(out var task)) TickTaskManager.AddTask(task);
    }

    public bool TryStartTask(out ProcessResourceTask task)
    {
        task = null;

        if (Spacebox.Network.GameSession.IsRemoteClient) return false;
        if (!HasPower) return false;
        if (!HasInput()) return false;
        if (IsRunning) return false;

        if (!GameAssets.TryGetRecipe(RecipeType, InputStorage.GetSlot(0, 0).Item.Id, out _recipe)) return false;
        if (_recipe == null) return false;

        if (!ValidateInput(InputStorage.GetSlot(0, 0), _recipe) || !ValidateOutput(OutputStorage.GetSlot(0, 0), _recipe))
        {
            Reset();
            return false;
        }

        StopTask();

        IsRunning = true;

        var ticksRequired = GetTicksWithEfficiency(_recipe.RequiredTicks);
        CraftTicks = (short)ticksRequired;
        CurrentTick = 0;

        task = new ProcessResourceTask(ticksRequired, this);
        Task = task;
        task.OnTick += OnTick;
        return true;
    }

    public void StopTask()
    {
        if (Task == null) return;

        Task.Stop();
        Task = null;
        CurrentTick = 0;
        CraftTicks = 0;
        IsRunning = false;
    }

    public void RestoreTaskProgress(short savedTicks)
    {
        if (!TryStartTask(out var task)) return;

        task.SetCurrentTick(savedTicks);
        CurrentTick = savedTicks;
        IsRunning = true;
        TickTaskManager.AddTask(task);
    }

    public void Craft()
    {
        if (_recipe == null)
        {
            Reset();
            return;
        }

        var inSlot = InputStorage.GetSlot(0, 0);
        var outSlot = OutputStorage.GetSlot(0, 0);

        if (!ValidateInput(inSlot, _recipe) || !ValidateOutput(outSlot, _recipe))
        {
            Reset();
            return;
        }

        var recipe = _recipe;
        inSlot.Count -= recipe.Ingredient.Quantity;

        if (outSlot.HasItem)
        {
            outSlot.Count += recipe.Product.Quantity;
            ProcessedItems += recipe.Product.Quantity;
        }
        else if (OutputStorage.TryAddItem(recipe.Product.Item, recipe.Product.Quantity))
        {
            ProcessedItems += recipe.Product.Quantity;
        }

        _craftedCount++;
        Host.MarkStateDirty();
        OnCrafted?.Invoke(this);

        if (!HasInput()) Reset();
    }

    private void OnTick()
    {
        if (!IsRunning) return;

        CurrentTick++;
        if (CurrentTick >= CraftTicks) CurrentTick = 0;
    }

    private void Reset()
    {
        CurrentTick = 0;
        CraftTicks = 0;
        IsRunning = false;
    }

    private void OnAnySlotWasChanged(Storage storage)
    {
        if (Host?.chunk != null) Host.chunk.IsModified = true;

        if (_recipe != null && (!ValidateInput(InputStorage.GetSlot(0, 0), _recipe) || !ValidateOutput(OutputStorage.GetSlot(0, 0), _recipe)))
            StopTask();

        ExchangeWithBuffer();
    }

    private void OnBufferChanged(Storage storage) => ExchangeWithBuffer();

    private void ExchangeWithBuffer()
    {
        if (_buffer == null || _exchanging || Host == null || Host.IsLoading || Spacebox.Network.GameSession.IsRemoteClient) return;

        _exchanging = true;
        try
        {
            ProcessingBuffer.PushOutput(OutputStorage.GetSlot(0, 0), _buffer.Storage);
            ProcessingBuffer.PullInput(InputStorage, _buffer.Storage, RecipeType);
        }
        finally
        {
            _exchanging = false;
        }

        if (!IsRunning) TryStart();
    }

    private static bool ValidateInput(ItemSlot inSlot, Recipe recipe)
    {
        return inSlot.HasItem && inSlot.Item.Id == recipe.Ingredient.Item.Id && inSlot.Count >= recipe.Ingredient.Quantity;
    }

    private static bool ValidateOutput(ItemSlot outSlot, Recipe recipe)
    {
        if (!outSlot.HasItem) return true;
        if (outSlot.Item.Id != recipe.Product.Item.Id) return false;
        return outSlot.Count + recipe.Product.Quantity <= recipe.Product.Item.StackSize;
    }

    private static void WriteSlot(CompoundTag root, string key, ItemSlot slot, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        if (!slot.HasItem) return;

        if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;
        root.Add(new IntTag(key, PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count)));
    }

    private void ReadSlot(CompoundTag tag, string key, Storage storage, string[] paletteItems)
    {
        if (!tag.TryGetValue<IntTag>(key, out var data)) return;

        PackingTools.UnpackShorts(data.Value, out short itemIndex, out short count);
        var item = itemIndex >= 0 && itemIndex < paletteItems.Length ? GameAssets.GetItemByFullID(paletteItems[itemIndex]) : null;
        if (item != null) SetStorageAfterLoadFromNBT(item, (byte)count, storage);
    }
}
