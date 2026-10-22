using Engine;
using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;
using Spacebox.Game.Tutorial;
using Spacebox.Network.Messages;
using Spacebox.Network.Players;

namespace Spacebox.Network.Items;

public static class ItemAuthority
{
    private static readonly Dictionary<int, TrackedPlayer> Players = new Dictionary<int, TrackedPlayer>();
    private static readonly Func<string, short?> ResolveItem = fullId => GameAssets.TryGetItemByFullID(fullId, out var item) ? item.Id : null;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<CraftClaimMessage>(OnCraftClaim);
        network.Messages.Register<TutorialRewardClaimMessage>(OnTutorialRewardClaim);
    }

    public static void OnJoin(int peer, string name, string playerJson)
    {
        if (!IsRemote(peer)) return;

        var start = StartInventory.FromGameSet();
        if (!PlayerInventoryJson.TryReadTotals(playerJson, ResolveItem, out var totals, out bool hasEquipment))
        {
            totals = StartInventory.Totals(start);
        }
        else if (!hasEquipment)
        {
            foreach (var pair in StartInventory.EquipmentOnly(start))
            {
                totals.TryGetValue(pair.Key, out int count);
                totals[pair.Key] = count + pair.Value;
            }
        }

        var ledger = new ItemLedger(totals);
        foreach (var reward in PlayerInventoryJson.ReadStrings(playerJson, PlayerInventoryJson.ClaimedRewardsField)) ledger.ClaimedRewards.Add(reward);
        Players[peer] = new TrackedPlayer(name, ledger);
    }

    public static void OnLeave(int peer) => Players.Remove(peer);

    public static string ReconcileForSave(int peer, string json)
    {
        if (string.IsNullOrEmpty(json) || !TryGetTracked(peer, out var player)) return json;
        if (!PlayerInventoryJson.TryReadTotals(json, ResolveItem, out var totals, out _)) return json;

        var excess = player.Ledger.ExcessOver(totals);
        if (excess.Count == 0) return json;

        Debug.Log($"[Items] {player.Name} left before the last changes were saved; removed {excess.Count} item type(s) the server had already taken");
        return WithServerFields(peer, PlayerInventoryJson.RemoveItems(json, ResolveItem, excess));
    }

    public static void Clear() => Players.Clear();

    public static bool IsTracked(int peer) => Players.TryGetValue(peer, out var player) && IsSurvival(player);

    public static int Available(int peer, short itemId) => TryGetTracked(peer, out var player) ? player.Ledger.Available(itemId) : int.MaxValue;

    public static bool TryLose(int peer, short itemId, int amount)
    {
        if (!TryGetTracked(peer, out var player)) return true;
        if (player.Ledger.TryLose(itemId, amount)) return true;

        Debug.Warning($"[Items] {player.Name} tried to use {amount} x {ItemName(itemId)} but has {player.Ledger.Available(itemId)}");
        return false;
    }

    public static bool TryLose(int peer, IReadOnlyList<ItemAmount> items)
    {
        if (!TryGetTracked(peer, out var player)) return true;
        if (player.Ledger.TryLose(items)) return true;

        Debug.Warning($"[Items] {player.Name} tried to use items the server does not know about");
        return false;
    }

    public static void Gain(int peer, short itemId, int amount)
    {
        if (TryGetTracked(peer, out var player)) player.Ledger.Gain(itemId, amount);
    }

    public static int Grant(int peer, short itemId, int amount) => Change(peer, new List<ItemAmount> { new ItemAmount { ItemId = itemId, Amount = amount } });

    public static int Change(int peer, List<ItemAmount> deltas)
    {
        if (!TryGetTracked(peer, out var player) || deltas.Count == 0) return 0;

        int seq = player.Ledger.Push(deltas, Time.Total);
        CheckPending(peer, player);
        return seq;
    }

    public static string AcceptSnapshot(int peer, string json, int appliedSeq)
    {
        if (!Players.TryGetValue(peer, out var player)) return json;
        if (!PlayerInventoryJson.TryReadTotals(json, ResolveItem, out var totals, out _)) return null;

        if (!IsSurvival(player)) player.Ledger.TrustNextSnapshot = true;
        var excess = player.Ledger.Snapshot(totals, appliedSeq, Time.Total);
        if (excess.Count > 0)
        {
            var remove = new List<ItemAmount>(excess.Count);
            foreach (var pair in excess) remove.Add(new ItemAmount { ItemId = pair.Key, Amount = pair.Value });

            int seq = player.Ledger.Push(Negate(remove), Time.Total);
            NetworkManager.Instance.Send(peer, new InventoryCorrectionMessage { Seq = seq, Remove = remove }, DeliveryMode.Reliable);
            Debug.Warning($"[Items] {player.Name} had items the server did not give: {Describe(remove)}; they were removed");
            json = PlayerInventoryJson.RemoveItems(json, ResolveItem, excess);
        }

        CheckPending(peer, player);
        return WithServerFields(peer, json);
    }

    public static string WithServerFields(int peer, string json)
    {
        if (string.IsNullOrEmpty(json) || !Players.TryGetValue(peer, out var player)) return json;
        return PlayerInventoryJson.WriteStrings(json, PlayerInventoryJson.ClaimedRewardsField, player.Ledger.ClaimedRewards);
    }

    public static void OnGameModeChanged(string name, GameMode mode)
    {
        if (mode != GameMode.Survival) return;

        foreach (var pair in Players)
        {
            if (!string.Equals(pair.Value.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            pair.Value.Ledger.TrustNextSnapshot = true;
            NetworkManager.Instance.Send(pair.Key, new PlayerDataRequestMessage(), DeliveryMode.Reliable);
        }
    }

    private static void OnCraftClaim(int sender, CraftClaimMessage message)
    {
        if (!NetworkManager.Instance.IsServer || !TryGetTracked(sender, out var player)) return;
        if (message.Times < 1 || !GameAssets.Blueprints.TryGetValue(message.ProductItemId, out var blueprint) || blueprint.Product?.Item == null) return;

        var scaled = blueprint.Clone();
        if (message.Times > 1) Blueprint.ScaleBlueprint(scaled, message.Times);

        var ingredients = new List<ItemAmount>(scaled.Ingredients.Length);
        foreach (var ingredient in scaled.Ingredients)
        {
            if (ingredient?.Item == null || Spacebox.Game.GUI.CraftingLogic.IsVirtualResource(ingredient.Item.Id_string)) continue;
            ingredients.Add(new ItemAmount { ItemId = ingredient.Item.Id, Amount = ingredient.Quantity });
        }

        if (!player.Ledger.TryLose(ingredients))
        {
            Debug.Warning($"[Items] {player.Name} crafted {scaled.Product.Item.Name} without the ingredients");
            return;
        }

        player.Ledger.Gain(scaled.Product.Item.Id, scaled.Product.Quantity);
    }

    private static void OnTutorialRewardClaim(int sender, TutorialRewardClaimMessage message)
    {
        if (!NetworkManager.Instance.IsServer || !TryGetTracked(sender, out var player)) return;

        string key = message.TutorialId + "/" + message.RewardKey;
        var rewards = TutorialRewardKeys.Find(TutorialLibrary.Find(message.TutorialId), message.RewardKey);
        if (rewards.Count == 0 || !player.Ledger.ClaimedRewards.Add(key)) return;

        foreach (var reward in rewards) player.Ledger.Gain(reward.ItemId, reward.Count);
    }

    private static void CheckPending(int peer, TrackedPlayer player)
    {
        if (player.Ledger.PendingCount <= ItemLedger.MaxPending) return;

        Debug.Warning($"[Items] The inventory of {player.Name} is out of sync, the player is disconnected");
        NetworkManager.Instance.Kick(peer, "Inventory out of sync, please join again");
    }

    private static bool TryGetTracked(int peer, out TrackedPlayer player)
    {
        return Players.TryGetValue(peer, out player) && IsSurvival(player);
    }

    private static bool IsSurvival(TrackedPlayer player) => PlayerGameModeService.Of(player.Name) == GameMode.Survival;

    private static bool IsRemote(int peer) => NetworkManager.Instance.IsServer && peer != NetworkManager.Instance.LocalPeerId;

    private static List<ItemAmount> Negate(List<ItemAmount> items)
    {
        var result = new List<ItemAmount>(items.Count);
        foreach (var item in items) result.Add(new ItemAmount { ItemId = item.ItemId, Amount = -item.Amount });
        return result;
    }

    private static string ItemName(short itemId) => GameAssets.TryGetItemById(itemId, out var item) ? item.Name : itemId.ToString();

    private static string Describe(List<ItemAmount> items)
    {
        var parts = new List<string>(items.Count);
        foreach (var item in items) parts.Add($"{item.Amount} x {ItemName(item.ItemId)}");
        return string.Join(", ", parts);
    }

    private sealed class TrackedPlayer
    {
        public TrackedPlayer(string name, ItemLedger ledger)
        {
            Name = name;
            Ledger = ledger;
        }

        public string Name { get; }
        public ItemLedger Ledger { get; }
    }
}
