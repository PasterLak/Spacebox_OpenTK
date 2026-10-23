namespace Spacebox.Game.GUI.Canvas.Windows;

public static class StorageButtons
{
    public const string CombineText = "><";
    public const string CombineTooltip = "Combine same stacks";
    public const string SortText = "A>z";
    public const string SortTooltip = "Sort all";

    public static void Combine(Storage storage)
    {
        if (storage != null) Storage.CombineStacks(storage);
    }

    public static void Sort(Storage storage)
    {
        if (storage == null) return;
        Storage.CombineStacks(storage);
        Storage.SortByStackCount(storage);
    }
}
