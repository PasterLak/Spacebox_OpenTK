using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Items;

public static class ItemSync
{
    public static int AppliedSeq { get; private set; }

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<InventoryCorrectionMessage>(OnCorrection);
    }

    public static void Applied(int seq)
    {
        if (seq > AppliedSeq) AppliedSeq = seq;
    }

    public static void Reset()
    {
        AppliedSeq = 0;
    }

    public static void ClaimCraft(Item product, int times)
    {
        var network = NetworkManager.Instance;
        if (!network.IsRemoteClient || product == null || times < 1) return;
        network.SendToServer(new CraftClaimMessage { ProductItemId = product.Id, Times = (byte)Math.Min(times, byte.MaxValue) }, DeliveryMode.Reliable);
    }

    public static void ClaimTutorialReward(string tutorialId, string rewardKey)
    {
        var network = NetworkManager.Instance;
        if (!network.IsRemoteClient || string.IsNullOrEmpty(tutorialId) || string.IsNullOrEmpty(rewardKey)) return;
        network.SendToServer(new TutorialRewardClaimMessage { TutorialId = tutorialId, RewardKey = rewardKey }, DeliveryMode.Reliable);
    }

    private static void OnCorrection(int sender, InventoryCorrectionMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var player = PanelUI.Player;
        if (player != null && message.Remove != null)
        {
            foreach (var amount in message.Remove)
            {
                if (!GameAssets.TryGetItemById(amount.ItemId, out var item)) continue;
                int left = amount.Amount;
                left -= RemoveFrom(player.Panel, item, left);
                left -= RemoveFrom(player.Inventory, item, left);
                RemoveFrom(player.Equipment.Storage, item, left);
            }
            Chat.Write("The server corrected your inventory", Color4.Orange);
        }

        Applied(message.Seq);
    }

    private static int RemoveFrom(Storage storage, Item item, int amount)
    {
        if (storage == null || amount <= 0) return 0;
        return Containers.ContainerSlots.RemoveUpTo(new List<ItemSlot>(storage.GetAllSlots()), item, amount);
    }
}
