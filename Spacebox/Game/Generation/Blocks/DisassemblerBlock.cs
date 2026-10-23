using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Disassembly;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class DisassemblerBlock : InteractiveBlock, IContainerBlock, IEntityBlockListener
{
    private readonly DisassemblerBlockData _data;
    private readonly DisassemblerInventory _inventory = new DisassemblerInventory(DisassemblyRecipes.TryGet);
    private readonly Action _startTicker;

    private SpaceEntity _entity;
    private TickTask _ticker;
    private DisassemblyRecipe _recipe;
    private bool _keepProgress;
    private volatile bool _wakeQueued;
    private bool _syncedPower;
    private byte _completedCount;
    private int _syncedCompletedCount = -1;

    public Storage InputStorage => _inventory.Input;
    public Storage OutputStorage => _inventory.Output;
    public DisassemblerInventory Inventory => _inventory;
    public DisassemblerStatus Status { get; private set; }
    public int Progress { get; private set; }
    public int RequiredTicks { get; private set; }
    public short CurrentItemId { get; private set; }
    public ushort Power => _data.Power;
    public bool HasPower => Spacebox.Network.GameSession.IsRemoteClient ? _syncedPower : IsActive;
    public float ProgressNormalized => RequiredTicks > 0 ? Math.Clamp(Progress / (float)RequiredTicks, 0f, 1f) : 0f;

    public Action<DisassemblerBlock> OnDisassembled;

    public DisassemblerBlock(BlockData blockData) : base(blockData)
    {
        _data = blockData as DisassemblerBlockData ?? new DisassemblerBlockData(blockData, null);
        _startTicker = StartTicker;

        //EFlags = ElectricalFlags.CanConsume;

        //ConsumptionRate = 15;
        EFlags = ElectricalFlags.CanConsume;
        ConsumptionRate = _data.Power;
        MaxPower = (short)Math.Clamp(_data.Power * 2, 300, 16000);
        CurrentPower = 0;

        InputStorage.OnDataWasChanged += OnStorageChanged;
        OutputStorage.OnDataWasChanged += OnStorageChanged;

        OnDestroy += DropContents;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        GameUi.Current?.Disassembler.Open(this, player);
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        _entity = chunk.SpaceEntity;
        Wake();
    }

    public void OnRemovedFromEntity()
    {
        _entity = null;
    }

    public string ContainerWindowId => DisassemblerUI.WindowId;

    public IReadOnlyList<Storage> GetContainerStorages() => [InputStorage, OutputStorage];

    public void WriteContainerState(NetWriter writer)
    {
        writer.Write((byte)Status);
        writer.Write(Progress);
        writer.Write(RequiredTicks);
        writer.Write(CurrentItemId);
        writer.Write(IsActive);
        writer.Write(_completedCount);
    }

    public void ReadContainerState(NetReader reader)
    {
        Status = (DisassemblerStatus)reader.ReadByte();
        Progress = reader.ReadInt();
        RequiredTicks = reader.ReadInt();
        CurrentItemId = reader.ReadShort();
        _syncedPower = reader.ReadBool();

        byte completed = reader.ReadByte();
        bool disassembled = _syncedCompletedCount >= 0 && completed != _syncedCompletedCount;
        _syncedCompletedCount = completed;
        if (disassembled) OnDisassembled?.Invoke(this);
    }

    public void ResetSyncedState()
    {
        _syncedCompletedCount = -1;
    }

    public void OnContainerChangedByNetwork()
    {
        Wake();
    }

    public void RestoreProgress(int progress)
    {
        Progress = Math.Max(0, progress);
        _keepProgress = Progress > 0;
    }

    protected override void OnActiveStateChanged(bool isActive)
    {
        base.OnActiveStateChanged(isActive);
        if (isActive) Wake();
    }

    private void OnStorageChanged(Storage storage)
    {
        if (chunk != null) chunk.IsModified = true;
        Wake();
    }

    private void Wake()
    {
        if (Spacebox.Network.GameSession.IsRemoteClient || _entity == null || _wakeQueued) return;
        if (_ticker != null && !_ticker.IsStopped) return;

        _wakeQueued = true;
        MainThreadDispatcher.Instance.Enqueue(_startTicker);
    }

    private void StartTicker()
    {
        _wakeQueued = false;
        if (_entity == null || (_ticker != null && !_ticker.IsStopped)) return;

        _ticker = new TickTask(1, Tick, true);
        TickTaskManager.AddTask(_ticker);
    }

    private void StopTicker()
    {
        _ticker?.Stop();
        _ticker = null;
    }

    private void Tick()
    {
        if (_entity == null || Durability == 0 || !IsActive)
        {
            StopTicker();
            return;
        }

        if ((_recipe == null || !_inventory.CanProcess(_recipe)) && !SelectRecipe())
        {
            StopTicker();
            return;
        }

        Progress++;
        if (Progress < RequiredTicks) return;

        Progress = 0;
        if (!_inventory.TryProcess(_recipe)) return;

        _completedCount++;
        OnDisassembled?.Invoke(this);
    }

    private bool SelectRecipe()
    {
        Status = _inventory.FindRecipe(out var recipe);

        if (recipe == null)
        {
            _recipe = null;
            Progress = 0;
            RequiredTicks = 0;
            CurrentItemId = 0;
            return false;
        }

        if (!_keepProgress && (_recipe == null || _recipe.Input.Id != recipe.Input.Id)) Progress = 0;
        _keepProgress = false;

        _recipe = recipe;
        RequiredTicks = Math.Max(1, (int)(recipe.DurationTicks / Math.Max(0.01f, _data.Efficiency)));
        Progress = Math.Min(Progress, RequiredTicks - 1);
        CurrentItemId = recipe.Input.Id;
        return true;
    }

    private void DropContents(HitInfo info)
    {
        if (!InputStorage.HasAnyItems() && !OutputStorage.HasAnyItems()) return;

        var combined = Storage.CombineStorages([InputStorage, OutputStorage], DisassemblerInventory.Width * 2, DisassemblerInventory.Height);
        InputStorage.Clear();
        OutputStorage.Clear();
        World.DropEffectManager?.DropStorage(combined, info.blockPositionWorld);
    }
}
