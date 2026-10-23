namespace Spacebox.Game.Player.Targeting;

public enum BodyHighlightKind
{
    None,
    Drill,
    Gift
}

public static class BodyHighlightRules
{
    public static float Range(Item heldItem, float giftRange)
    {
        if (heldItem is DrillItem drill) return Math.Max(drill.Range, giftRange);
        return ItemReceiver.CanReceive(heldItem) ? giftRange : 0f;
    }

    public static BodyHighlightKind Resolve(object owner, Item heldItem, float distance, float giftRange, bool networkActive)
    {
        if (owner == null || heldItem == null) return BodyHighlightKind.None;
        if (heldItem is DrillItem drill && owner is IDrillable && distance <= drill.Range) return BodyHighlightKind.Drill;
        if (networkActive && owner is RemoteAstronaut && ItemReceiver.CanReceive(heldItem) && distance <= giftRange) return BodyHighlightKind.Gift;
        return BodyHighlightKind.None;
    }
}
