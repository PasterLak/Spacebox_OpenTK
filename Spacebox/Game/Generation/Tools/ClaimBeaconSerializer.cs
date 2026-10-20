using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Tools;

public class ClaimBeaconSerializer : IBlockSerializer
{
    public string TagName => "claim_beacon_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var beacon = (ClaimBeaconBlock)block;
        var slot = beacon.PaymentStorage.GetSlot(0, 0);
        if (beacon.IsDefaultState && !slot.HasItem) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new StringTag("owner", beacon.Owner));
        root.Add(new FloatTag("remaining", beacon.RemainingSeconds));

        var members = new ListTag("members", TagType.String);
        for (int i = 0; i < beacon.Members.Count; i++) members.Add(new StringTag(null, beacon.Members[i]));
        root.Add(members);

        if (slot.HasItem)
        {
            if (itemsPalette.TryAdd(slot.Item.Id_string, indexInPalette)) indexInPalette++;
            root.Add(new IntTag("slot_data", PackingTools.PackShorts(itemsPalette[slot.Item.Id_string], slot.Count)));
        }

        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var beacon = (ClaimBeaconBlock)block;

        string owner = tag.TryGetValue<StringTag>("owner", out var ownerTag) ? ownerTag.Value : "";
        float remaining = tag.TryGetValue<FloatTag>("remaining", out var remainingTag) ? remainingTag.Value : 0f;
        var members = new List<string>();
        if (tag.TryGetValue<ListTag>("members", out var membersTag))
        {
            foreach (var member in membersTag)
            {
                if (member is StringTag name) members.Add(name.Value);
            }
        }

        beacon.Restore(owner, members, remaining);

        if (tag.TryGetValue<IntTag>("slot_data", out var slotData))
        {
            PackingTools.UnpackShorts(slotData.Value, out short itemIndex, out short count);
            if (itemIndex >= 0 && itemIndex < paletteItems.Length)
            {
                var item = GameAssets.GetItemByFullID(paletteItems[itemIndex]);
                if (item != null) beacon.PaymentStorage.SetSlot(0, 0, item, (byte)count);
            }
        }
    }
}
