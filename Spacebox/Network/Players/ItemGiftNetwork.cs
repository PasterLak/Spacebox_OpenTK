using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public static class ItemGiftNetwork
{
    public const float MaxDistance = 2f;
    public const float ServerDistanceTolerance = 1.5f;
    public const float Timeout = 5f;
    public const int MaxReasonLength = 80;

    private static readonly Color4 InfoColor = new Color4(0.6f, 1f, 0.6f, 1f);
    private static readonly Color4 FailColor = new Color4(1f, 0.6f, 0.4f, 1f);

    private readonly struct PendingGift
    {
        public readonly short ItemId;
        public readonly double SentAt;
        public readonly bool Consumed;
        public readonly string TargetName;

        public PendingGift(short itemId, double sentAt, bool consumed, string targetName)
        {
            ItemId = itemId;
            SentAt = sentAt;
            Consumed = consumed;
            TargetName = targetName;
        }
    }

    private static readonly Dictionary<int, PendingGift> _pending = new Dictionary<int, PendingGift>();
    private static readonly List<int> _expired = new List<int>();
    private static int _nextRequestId = 1;

    public static bool TryGive(LocalAstronaut giver, ItemSlot slot)
    {
        if (giver == null || slot == null || !slot.HasItem || !NetworkManager.Instance.IsActive) return false;

        var item = slot.Item;
        if (!ItemReceiver.CanReceive(item)) return false;
        if (!PlayerTargeting.TryFindLookedAt(giver, MaxDistance, out var target)) return false;

        var targetNetwork = AstronautNetwork.Find(target);
        if (targetNetwork == null || !targetNetwork.IsAlive) return false;

        int requestId = _nextRequestId++;
        bool consumed = giver.GameMode == GameMode.Survival;
        _pending[requestId] = new PendingGift(item.Id, Time.Total, consumed, target.PlayerName);
        if (consumed) slot.DropOne();

        targetNetwork.RequestGift(item.Id, requestId);
        return true;
    }

    public static void OnResult(int requestId, bool success, string reason)
    {
        if (!_pending.Remove(requestId, out var gift)) return;

        string itemName = GetItem(gift.ItemId)?.Name ?? "item";
        if (success)
        {
            Chat.Write($"You gave {itemName} to {gift.TargetName}", InfoColor);
            return;
        }

        Refund(gift);
        Chat.Write(string.IsNullOrEmpty(reason) ? $"{gift.TargetName} could not take {itemName}" : $"{gift.TargetName}: {reason}", FailColor);
    }

    public static void Update()
    {
        if (_pending.Count == 0) return;

        _expired.Clear();
        foreach (var pair in _pending)
        {
            if (Time.Total - pair.Value.SentAt >= Timeout) _expired.Add(pair.Key);
        }

        foreach (int requestId in _expired)
            OnResult(requestId, false, "no answer");
    }

    public static void Reset()
    {
        _pending.Clear();
    }

    public static string Clamp(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "";
        return reason.Length > MaxReasonLength ? reason.Substring(0, MaxReasonLength) : reason;
    }

    private static void Refund(PendingGift gift)
    {
        if (!gift.Consumed) return;

        var item = GetItem(gift.ItemId);
        var player = Spacebox.Game.Generation.World.Instance?.Player;
        if (item == null || player == null) return;

        if (player.Panel.TryAddItem(item, 1) || player.Inventory.TryAddItem(item, 1)) return;
        Spacebox.Game.Generation.World.DropEffectManager?.DropItem(player.Position, player.Front, 1f, item, 1);
    }

    private static Item GetItem(short id)
    {
        return GameAssets.Items.TryGetValue(id, out var item) ? item : null;
    }
}
