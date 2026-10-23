namespace Spacebox.Game.Player.GameModes;

public static class HotbarLayout
{
    public const int MaxRows = 3;
    public const int MaxSlotsPerRow = 20;
    public const int MaxSlots = MaxRows * MaxSlotsPerRow;
    public const int NumberKeys = 10;
    public const int DefaultSlotsPerRow = 10;
    public const int DefaultRows = 1;

    public static int ClampRows(int rows) => Math.Clamp(rows, 1, MaxRows);

    public static int ClampSlotsPerRow(int slots) => Math.Clamp(slots, 1, MaxSlotsPerRow);

    public static int ActiveSlots(GameModeRules rules) => ClampRows(rules.HotbarRows) * ClampSlotsPerRow(rules.HotbarSlots);

    public static int SlotForNumberKey(int keyIndex, int activeSlots)
    {
        if (keyIndex < 0 || keyIndex >= NumberKeys) return -1;
        return keyIndex < activeSlots ? keyIndex : -1;
    }

    public static int Step(int selected, int delta, int activeSlots)
    {
        if (activeSlots <= 0) return 0;
        int next = (selected + delta) % activeSlots;
        return next < 0 ? next + activeSlots : next;
    }

    public static int Clamp(int selected, int activeSlots) => selected >= 0 && selected < activeSlots ? selected : 0;
}
