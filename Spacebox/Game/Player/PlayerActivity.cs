namespace Spacebox.Game.Player;

public enum PlayerActivityKind : byte
{
    BlockPlaced,
    BlockDestroyed,
    ItemPickedUp,
    ItemCrafted,
    ItemProcessed,
    ItemUsed,
    BlockUsed,
    ShipPartBound,
    WaypointCreated
}

public readonly struct PlayerActivityEvent
{
    public readonly PlayerActivityKind Kind;
    public readonly short Id;
    public readonly int Count;
    public readonly string Tag;

    public PlayerActivityEvent(PlayerActivityKind kind, short id, int count, string tag)
    {
        Kind = kind;
        Id = id;
        Count = count;
        Tag = tag;
    }
}

public static class PlayerActivity
{
    public static event Action<PlayerActivityEvent> Happened;

    public static void Raise(PlayerActivityKind kind, short id = 0, int count = 1, string tag = null)
    {
        if (count <= 0) return;
        Happened?.Invoke(new PlayerActivityEvent(kind, id, count, tag));
    }

    public static void RaiseBlock(PlayerActivityKind kind, short blockId)
    {
        if (Happened == null) return;

        var item = GameAssets.Blocks.TryGetValue(blockId, out var data) ? data.AsItem : null;
        Raise(kind, item?.Id ?? blockId);
    }
}
