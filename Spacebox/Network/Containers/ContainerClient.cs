using Engine;
using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public sealed class ContainerClient
{
    private sealed class OpenContainer
    {
        public ContainerKey Key;
        public IContainer Block;
        public IReadOnlyList<Storage> Storages;
        public bool HasSnapshot;
        public bool IsClosing;
        public SlotState[] Confirmed;
        public int ConfirmedVersion;
        public SlotState[] Predicted;
        public int PredictedVersion;
        public int Pending;
        public float OpenRetryTimer;
    }

    private const float OpenRetryInterval = 1f;

    private readonly Dictionary<ContainerKey, OpenContainer> _open = new Dictionary<ContainerKey, OpenContainer>();
    private readonly List<ContainerKey> _closeBuffer = new List<ContainerKey>();
    private readonly NetReader _stateReader = new NetReader();

    public bool HasPendingChanges
    {
        get
        {
            foreach (var container in _open.Values)
            {
                if (container.Pending > 0) return true;
            }
            return false;
        }
    }

    public bool IsMirrored(Storage storage)
    {
        foreach (var container in _open.Values)
        {
            for (int i = 0; i < container.Storages.Count; i++)
            {
                if (container.Storages[i] == storage) return true;
            }
        }
        return false;
    }

    public void Open(IContainer block, ContainerKey key)
    {
        if (_open.TryGetValue(key, out var existing))
        {
            existing.IsClosing = false;
            return;
        }

        _open[key] = new OpenContainer { Key = key, Block = block, Storages = block.GetContainerStorages() };
        NetworkManager.Instance.SendToServer(new ContainerOpenMessage { Key = key });
    }

    public void Update()
    {
        _closeBuffer.Clear();
        OpenContainer pendingContainer = null;
        foreach (var container in _open.Values)
        {
            if (container.Pending > 0) pendingContainer = container;
        }

        foreach (var container in _open.Values)
        {
            if (!container.IsClosing && !UIManager.IsOpen(container.Block.ContainerWindowId)) container.IsClosing = true;

            if (container.IsClosing)
            {
                if (container.Pending == 0) _closeBuffer.Add(container.Key);
                continue;
            }

            if (!container.HasSnapshot)
            {
                container.OpenRetryTimer += Time.Delta;
                if (container.OpenRetryTimer >= OpenRetryInterval)
                {
                    container.OpenRetryTimer = 0f;
                    NetworkManager.Instance.SendToServer(new ContainerOpenMessage { Key = container.Key });
                }
                continue;
            }

            var current = ContainerSlots.Read(container.Storages);
            if (ContainerSlots.SequenceEqual(current, container.Predicted)) continue;
            if (pendingContainer != null && pendingContainer != container) continue;

            NetworkManager.Instance.SendToServer(new ContainerChangeMessage
            {
                Key = container.Key,
                BaseVersion = container.PredictedVersion,
                Desired = current,
                Deltas = ContainerSlots.Deltas(container.Predicted, current)
            });

            container.Predicted = current;
            container.PredictedVersion++;
            container.Pending++;
            pendingContainer = container;
        }

        for (int i = 0; i < _closeBuffer.Count; i++)
        {
            NetworkManager.Instance.SendToServer(new ContainerCloseMessage { Key = _closeBuffer[i] });
            _open.Remove(_closeBuffer[i]);
        }
    }

    public void OnSnapshot(ContainerSnapshotMessage message)
    {
        if (!_open.TryGetValue(message.Key, out var container)) return;

        container.Confirmed = message.Slots;
        container.ConfirmedVersion = message.Version;

        _stateReader.Set(message.State, 0, message.State.Length);
        try
        {
            container.Block.ReadContainerState(_stateReader);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Containers] Bad container state: {ex.Message}");
        }

        if (!container.HasSnapshot || container.Pending == 0)
        {
            container.HasSnapshot = true;
            ApplyConfirmed(container);
        }
    }

    public void OnResult(ContainerChangeResultMessage message)
    {
        if (!_open.TryGetValue(message.Key, out var container)) container = null;

        RemoveDeniedFromPlayer(message.DeniedRemovals);
        ReturnRejectedToPlayer(message.RejectedAdditions);

        if (container == null) return;

        container.Pending = Math.Max(0, container.Pending - 1);
        if (container.Pending > 0) return;

        container.PredictedVersion = message.NewVersion;
        if (container.ConfirmedVersion >= message.NewVersion) ApplyConfirmed(container);
    }

    public void Clear()
    {
        _open.Clear();
    }

    private static void ApplyConfirmed(OpenContainer container)
    {
        if (container.Confirmed == null) return;

        if (!ContainerSlots.SequenceEqual(ContainerSlots.Read(container.Storages), container.Confirmed))
            ContainerSlots.Write(container.Storages, container.Confirmed);

        container.Predicted = container.Confirmed;
        container.PredictedVersion = container.ConfirmedVersion;
    }

    private static void RemoveDeniedFromPlayer(List<ItemAmount> denied)
    {
        var player = PanelUI.Player;
        if (player == null) return;

        for (int i = 0; i < denied.Count; i++)
        {
            if (!GameAssets.TryGetItemById(denied[i].ItemId, out var item)) continue;

            int left = denied[i].Amount;
            left -= RemoveFrom(player.Panel, item, left);
            left -= RemoveFrom(player.Inventory, item, left);
            if (left > 0) Debug.Warning($"[Containers] Could not take back {left} x {item.Name}");
        }
    }

    private static int RemoveFrom(Storage storage, Item item, int amount)
    {
        if (amount <= 0) return 0;
        return ContainerSlots.RemoveUpTo(new List<ItemSlot>(storage.GetAllSlots()), item, amount);
    }

    private static void ReturnRejectedToPlayer(List<ItemAmount> rejected)
    {
        var player = PanelUI.Player;
        if (player == null) return;

        for (int i = 0; i < rejected.Count; i++)
        {
            if (!GameAssets.TryGetItemById(rejected[i].ItemId, out var item)) continue;

            int left = rejected[i].Amount;
            left -= ContainerSlots.AddUpTo(new List<ItemSlot>(player.Panel.GetAllSlots()), item, left);
            if (left > 0) left -= ContainerSlots.AddUpTo(new List<ItemSlot>(player.Inventory.GetAllSlots()), item, left);
            if (left > 0) World.DropEffectManager?.DropItem(player.Position, player.Front, 1f, item, left, 2f, 4f);
        }
    }
}
