using Engine;
using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public sealed class ContainerServer
{
    private const float StateInterval = 0.25f;
    private const float MaxOpenDistance = 12f;
    private const float MaxChangeDistance = 16f;
    private const int MaxDeltaAmount = 100000;

    private readonly PeerRateLimiter _deposits = new PeerRateLimiter(4096, 1024);

    private sealed class Entry
    {
        public ContainerKey Key;
        public IContainer Block;
        public IReadOnlyList<Storage> Storages;
        public readonly HashSet<int> Viewers = new HashSet<int>();
        public int Version;
        public bool IsDirty;
        public bool IsSuppressed;
        public byte[] LastState = Array.Empty<byte>();
        public Action<Storage> Handler;
    }

    private readonly Dictionary<ContainerKey, Entry> _entries = new Dictionary<ContainerKey, Entry>();
    private readonly List<ContainerKey> _removeBuffer = new List<ContainerKey>();
    private readonly NetWriter _stateWriter = new NetWriter(64);
    private float _stateTimer;

    public void Open(int peer, ContainerKey key)
    {
        if (!IsInReach(peer, key, MaxOpenDistance)) return;
        if (!ContainerResolver.TryResolve(key, out var block)) return;

        if (!_entries.TryGetValue(key, out var entry) || entry.Block != block)
        {
            if (entry != null) Unsubscribe(entry);
            entry = CreateEntry(key, block);
            _entries[key] = entry;
        }

        entry.Viewers.Add(peer);
        NetworkManager.Instance.Send(peer, CreateSnapshot(entry));
    }

    public void Close(int peer, ContainerKey key)
    {
        if (!_entries.TryGetValue(key, out var entry)) return;

        entry.Viewers.Remove(peer);
        if (entry.Viewers.Count == 0) Remove(key, entry);
    }

    public static bool IsInReach(int peer, ContainerKey key, float distance)
    {
        if (peer == NetworkManager.Instance.LocalPeerId) return true;
        var player = Combat.PlayerHitboxes.FindPlayer(peer);
        if (player == null || !ContainerResolver.TryGetWorldPosition(key, out var position)) return false;
        return OpenTK.Mathematics.Vector3.DistanceSquared(player.Position, position) <= distance * distance;
    }

    private static double AddedAmount(List<ItemAmount> deltas)
    {
        long added = 0;
        if (deltas == null) return 0;
        for (int i = 0; i < deltas.Count; i++)
        {
            if (deltas[i].Amount > MaxDeltaAmount || deltas[i].Amount < -MaxDeltaAmount) return double.PositiveInfinity;
            if (deltas[i].Amount > 0) added += deltas[i].Amount;
        }
        return added;
    }

    public void RemovePeer(int peer)
    {
        _deposits.Forget(peer);
        _removeBuffer.Clear();
        foreach (var pair in _entries)
        {
            pair.Value.Viewers.Remove(peer);
            if (pair.Value.Viewers.Count == 0) _removeBuffer.Add(pair.Key);
        }

        for (int i = 0; i < _removeBuffer.Count; i++) Remove(_removeBuffer[i], _entries[_removeBuffer[i]]);
    }

    public void HandleChange(int peer, ContainerChangeMessage message)
    {
        message.Deltas = ContainerDeltas.Normalize(message.Deltas);
        var denied = new List<ItemAmount>();
        var rejected = new List<ItemAmount>();

        if (!_entries.TryGetValue(message.Key, out var entry) || !entry.Viewers.Contains(peer) || !IsStillValid(entry) ||
            !IsInReach(peer, message.Key, MaxChangeDistance) || !HasDeposits(peer, message.Deltas) || !_deposits.TryConsume(peer, Time.Total, AddedAmount(message.Deltas)))
        {
            RejectAll(message.Deltas, denied, rejected);
            NetworkManager.Instance.Send(peer, new ContainerChangeResultMessage { Key = message.Key, NewVersion = entry?.Version ?? 0, DeniedRemovals = denied, RejectedAdditions = rejected });
            return;
        }

        var current = ContainerSlots.Read(entry.Storages);
        entry.IsSuppressed = true;

        if (message.BaseVersion == entry.Version && IsValidLayout(entry.Storages, current, message.Desired, message.Deltas))
            ContainerSlots.Write(entry.Storages, message.Desired);
        else
            Merge(entry.Storages, message.Deltas, denied, rejected);

        entry.IsSuppressed = false;
        entry.Version++;
        entry.IsDirty = true;

        ApplyToPlayer(peer, message.Deltas, denied, rejected);
        NetworkManager.Instance.Send(peer, new ContainerChangeResultMessage { Key = message.Key, NewVersion = entry.Version, DeniedRemovals = denied, RejectedAdditions = rejected });
        entry.Block.OnContainerChangedByNetwork();
    }

    private static bool HasDeposits(int peer, List<ItemAmount> deltas)
    {
        if (deltas == null || !Items.ItemAuthority.IsTracked(peer)) return true;

        var deposits = new List<ItemAmount>(deltas.Count);
        for (int i = 0; i < deltas.Count; i++)
        {
            if (deltas[i].Amount > 0) deposits.Add(deltas[i]);
        }

        for (int i = 0; i < deposits.Count; i++)
        {
            if (Items.ItemAuthority.Available(peer, deposits[i].ItemId) < deposits[i].Amount) return false;
        }
        return true;
    }

    private static void ApplyToPlayer(int peer, List<ItemAmount> deltas, List<ItemAmount> denied, List<ItemAmount> rejected)
    {
        if (deltas == null || !Items.ItemAuthority.IsTracked(peer)) return;

        for (int i = 0; i < deltas.Count; i++)
        {
            var delta = deltas[i];
            if (delta.Amount < 0) Items.ItemAuthority.Gain(peer, delta.ItemId, -delta.Amount - AmountOf(denied, delta.ItemId));
            else if (delta.Amount > 0) Items.ItemAuthority.TryLose(peer, delta.ItemId, delta.Amount - AmountOf(rejected, delta.ItemId));
        }
    }

    private static int AmountOf(List<ItemAmount> amounts, short itemId)
    {
        int total = 0;
        for (int i = 0; i < amounts.Count; i++)
        {
            if (amounts[i].ItemId == itemId) total += amounts[i].Amount;
        }
        return total;
    }

    public bool TryTake(ContainerKey key, short itemId, int amount)
    {
        if (amount <= 0 || !GameAssets.TryGetItemById(itemId, out var item)) return false;
        if (!ContainerResolver.TryResolve(key, out var block)) return false;

        var slots = ContainerSlots.GetSlots(block.GetContainerStorages());
        int available = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].HasItem && slots[i].Item.Id == itemId) available += slots[i].Count;
        }

        if (available < amount) return false;

        ContainerSlots.RemoveUpTo(slots, item, amount);
        return true;
    }

    public void Update()
    {
        _stateTimer += Time.Delta;
        bool checkState = _stateTimer >= StateInterval;
        if (checkState) _stateTimer = 0f;

        _removeBuffer.Clear();

        foreach (var pair in _entries)
        {
            var entry = pair.Value;
            if (!IsStillValid(entry))
            {
                _removeBuffer.Add(pair.Key);
                continue;
            }

            if (checkState && !entry.IsDirty && StateChanged(entry)) entry.IsDirty = true;
            if (!entry.IsDirty) continue;

            entry.IsDirty = false;
            var snapshot = CreateSnapshot(entry);
            foreach (var viewer in entry.Viewers) NetworkManager.Instance.Send(viewer, snapshot);
        }

        for (int i = 0; i < _removeBuffer.Count; i++) Remove(_removeBuffer[i], _entries[_removeBuffer[i]]);
    }

    public void Clear()
    {
        foreach (var entry in _entries.Values) Unsubscribe(entry);
        _entries.Clear();
    }

    private Entry CreateEntry(ContainerKey key, IContainer block)
    {
        var entry = new Entry { Key = key, Block = block, Storages = block.GetContainerStorages() };
        entry.Handler = _ =>
        {
            if (!entry.IsSuppressed) entry.Version++;
            entry.IsDirty = true;
        };

        for (int i = 0; i < entry.Storages.Count; i++) entry.Storages[i].OnDataWasChanged += entry.Handler;
        return entry;
    }

    private void Remove(ContainerKey key, Entry entry)
    {
        Unsubscribe(entry);
        _entries.Remove(key);
    }

    private static void Unsubscribe(Entry entry)
    {
        for (int i = 0; i < entry.Storages.Count; i++) entry.Storages[i].OnDataWasChanged -= entry.Handler;
    }

    private static bool IsStillValid(Entry entry)
    {
        return ContainerResolver.TryResolve(entry.Key, out var block) && block == entry.Block;
    }

    private ContainerSnapshotMessage CreateSnapshot(Entry entry)
    {
        return new ContainerSnapshotMessage
        {
            Key = entry.Key,
            Version = entry.Version,
            Slots = ContainerSlots.Read(entry.Storages),
            State = WriteState(entry)
        };
    }

    private byte[] WriteState(Entry entry)
    {
        _stateWriter.Reset();
        entry.Block.WriteContainerState(_stateWriter);
        entry.LastState = _stateWriter.ToArray();
        return entry.LastState;
    }

    private bool StateChanged(Entry entry)
    {
        _stateWriter.Reset();
        entry.Block.WriteContainerState(_stateWriter);
        return !_stateWriter.Span.SequenceEqual(entry.LastState);
    }

    private static bool IsValidLayout(IReadOnlyList<Storage> storages, SlotState[] current, SlotState[] desired, List<ItemAmount> deltas)
    {
        if (desired == null || desired.Length != current.Length) return false;

        var slots = ContainerSlots.GetSlots(storages);
        for (int i = 0; i < desired.Length; i++)
        {
            if (desired[i].IsEmpty) continue;
            if (!GameAssets.TryGetItemById(desired[i].ItemId, out var item)) return false;
            if (desired[i].Count > item.StackSize) return false;
            if (!slots[i].CanAccept(item) && !(slots[i].HasItem && slots[i].Item.Id == item.Id)) return false;
        }

        var before = ContainerSlots.Totals(current);
        var after = ContainerSlots.Totals(desired);
        var expected = new Dictionary<short, int>();
        for (int i = 0; i < deltas.Count; i++) expected[deltas[i].ItemId] = deltas[i].Amount;

        foreach (var id in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(id, out int oldCount);
            after.TryGetValue(id, out int newCount);
            expected.TryGetValue(id, out int delta);
            if (newCount - oldCount != delta) return false;
        }

        return true;
    }

    private static void Merge(IReadOnlyList<Storage> storages, List<ItemAmount> deltas, List<ItemAmount> denied, List<ItemAmount> rejected)
    {
        var slots = ContainerSlots.GetSlots(storages);

        for (int i = 0; i < deltas.Count; i++)
        {
            var delta = deltas[i];
            if (delta.Amount >= 0) continue;

            int wanted = -delta.Amount;
            int removed = GameAssets.TryGetItemById(delta.ItemId, out var item) ? ContainerSlots.RemoveUpTo(slots, item, wanted) : 0;
            if (removed < wanted) denied.Add(new ItemAmount { ItemId = delta.ItemId, Amount = wanted - removed });
        }

        for (int i = 0; i < deltas.Count; i++)
        {
            var delta = deltas[i];
            if (delta.Amount <= 0) continue;

            int added = GameAssets.TryGetItemById(delta.ItemId, out var item) ? ContainerSlots.AddUpTo(slots, item, delta.Amount) : 0;
            if (added < delta.Amount) rejected.Add(new ItemAmount { ItemId = delta.ItemId, Amount = delta.Amount - added });
        }
    }

    private static void RejectAll(List<ItemAmount> deltas, List<ItemAmount> denied, List<ItemAmount> rejected)
    {
        for (int i = 0; i < deltas.Count; i++)
        {
            if (deltas[i].Amount < 0) denied.Add(new ItemAmount { ItemId = deltas[i].ItemId, Amount = -deltas[i].Amount });
            else if (deltas[i].Amount > 0) rejected.Add(deltas[i]);
        }
    }
}
