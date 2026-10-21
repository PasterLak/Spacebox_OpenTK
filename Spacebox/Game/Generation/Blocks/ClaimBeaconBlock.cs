using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Protection;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public class ClaimBeaconBlock : InteractiveBlock, IContainerBlock, IEntityBlockListener, INetworkStateBlock
{
    public const float StateSyncInterval = 5f;

    private readonly List<string> _members = new List<string>();
    private float _syncTimer;
    private bool _warned;

    public SpaceEntity Entity { get; private set; }
    public Vector3i LocalCell { get; private set; }
    public string Owner { get; private set; } = "";
    public IReadOnlyList<string> Members => _members;
    public float RemainingSeconds { get; private set; }
    public Storage PaymentStorage { get; } = new Storage(1, 1);

    public bool IsClaimActive => ClaimSettings.Current.Enabled && RemainingSeconds > 0f && Owner.Length > 0;
    public bool CoversWholeEntity => Entity != null && Entity.IsDynamic();
    public bool IsDefaultState => Owner.Length == 0 && _members.Count == 0 && RemainingSeconds <= 0f;

    public ClaimBeaconBlock(BlockData blockData) : base(blockData)
    {
        PaymentStorage.Name = "Payment";
        var slot = PaymentStorage.GetSlot(0, 0);
        slot.Name = "Payment";
        slot.Filter = IsPayment;
        PaymentStorage.OnDataWasChanged += _ => MarkModified();
        OnDestroy += DropPayment;
    }

    public void OnAddedToEntity(Chunk chunk, Vector3i localCell)
    {
        this.chunk = chunk;
        Entity = chunk.SpaceEntity;
        LocalCell = localCell;
        ClaimRegistry.Register(this);
    }

    public void OnRemovedFromEntity()
    {
        ClaimRegistry.Unregister(this);
        Entity = null;
    }

    public override void Use(LocalAstronaut player, ref HitInfo hit)
    {
        base.Use(player, ref hit);
        ClaimBeaconUI.Open(this, player);
    }

    public bool IsAllowed(string playerName) => ClaimMembership.IsAllowed(Owner, _members, playerName);

    public bool Covers(Vector3i cell) => CoversWholeEntity || ClaimZone.Contains(LocalCell, ClaimSettings.Current.Radius, cell);

    public void SetOwner(string owner)
    {
        Owner = ClaimMembership.Sanitize(owner);
        MarkStateDirty();
        MarkModified();
        if (Entity != null) ClaimRegistry.NotifyStateChanged(this);
    }

    public bool TryAddMember(string name)
    {
        if (!ClaimMembership.TryAdd(_members, Owner, name, ClaimSettings.Current.MaxMembers)) return false;
        MarkStateDirty();
        MarkModified();
        return true;
    }

    public bool RemoveMember(string name)
    {
        if (!ClaimMembership.Remove(_members, name)) return false;
        MarkStateDirty();
        MarkModified();
        return true;
    }

    public void Restore(string owner, IEnumerable<string> members, float remainingSeconds)
    {
        Owner = ClaimMembership.Sanitize(owner);
        _members.Clear();
        if (members != null)
        {
            foreach (var member in members) ClaimMembership.TryAdd(_members, Owner, member, ClaimSettings.MaxMembersLimit);
        }
        RemainingSeconds = MathF.Max(0f, remainingSeconds);
        UpdateEmission();
    }

    public void Update(float deltaTime, float burnRate)
    {
        if (Spacebox.Network.GameSession.IsRemoteClient || Owner.Length == 0) return;

        var settings = ClaimSettings.Current;
        bool wasActive = IsClaimActive;
        bool changed = TakePayment(settings);

        if (RemainingSeconds > 0f)
        {
            RemainingSeconds = MathF.Max(0f, RemainingSeconds - deltaTime * burnRate);
            MarkModified();
        }

        WarnOwner(settings);

        _syncTimer += deltaTime;
        if (changed || wasActive != IsClaimActive || _syncTimer >= StateSyncInterval)
        {
            _syncTimer = 0f;
            UpdateEmission();
            MarkStateDirty();
            if (wasActive != IsClaimActive) ClaimRegistry.NotifyStateChanged(this);
        }
    }

    public string ContainerWindowId => "claimBeacon";

    public IReadOnlyList<Storage> GetContainerStorages() => [PaymentStorage];

    public void WriteContainerState(NetWriter writer) => WriteNetworkState(writer);

    public void ReadContainerState(NetReader reader) => ReadNetworkState(reader);

    public void OnContainerChangedByNetwork()
    {
    }

    public void WriteNetworkState(NetWriter writer)
    {
        writer.Write(Owner);
        writer.Write(_members.Count);
        for (int i = 0; i < _members.Count; i++) writer.Write(_members[i]);
        writer.Write(RemainingSeconds);
    }

    public void ReadNetworkState(NetReader reader)
    {
        string owner = reader.ReadString();
        int count = Math.Clamp(reader.ReadInt(), 0, ClaimSettings.MaxMembersLimit);
        var members = new string[count];
        for (int i = 0; i < count; i++) members[i] = reader.ReadString();
        bool wasActive = IsClaimActive;
        Restore(owner, members, reader.ReadFloat());
        if (wasActive != IsClaimActive) ClaimRegistry.NotifyStateChanged(this);
    }

    private bool TakePayment(ClaimSettings settings)
    {
        var slot = PaymentStorage.GetSlot(0, 0);
        if (!slot.HasItem) return false;

        int seconds = ClaimCost.SecondsFor(settings, slot.Item.Id_string);
        if (!ClaimCost.CanAdd(RemainingSeconds, seconds, settings.MaxStoredSeconds)) return false;

        slot.TakeOne();
        RemainingSeconds += seconds;
        _warned = false;
        MarkModified();
        return true;
    }

    private void WarnOwner(ClaimSettings settings)
    {
        if (RemainingSeconds <= 0f || RemainingSeconds > settings.WarningSeconds)
        {
            if (RemainingSeconds > settings.WarningSeconds) _warned = false;
            return;
        }

        if (_warned) return;
        _warned = true;
        Spacebox.Network.ChatService.SendToPlayer(Owner, $"Your claim beacon has {ClaimCost.FormatDuration(RemainingSeconds)} of protection left. Pay with ingots to keep it.", OpenTK.Mathematics.Color4.Orange);
    }

    private bool IsPayment(Item item) => item != null && ClaimCost.SecondsFor(ClaimSettings.Current, item.Id_string) > 0;

    private void UpdateEmission() => SetEmission(IsClaimActive, true);

    private void MarkStateDirty() => Spacebox.Network.BlockStateNetwork.MarkDirty(Entity, LocalCell, this);

    private void DropPayment(HitInfo info)
    {
        if (!PaymentStorage.HasAnyItems()) return;
        World.DropEffectManager?.DropStorage(PaymentStorage, info.blockPositionWorld);
    }

    private void MarkModified()
    {
        if (chunk != null) chunk.IsModified = true;
    }
}
