using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class PanelUI
{
    private static PanelController Panel => GameUi.Current?.Panel;

    public static LocalAstronaut Player => Panel?.Player;

    public static Item HeldItem => Panel?.HeldItem;

    public static bool AreHandsBlocked => Panel?.AreHandsBlocked ?? false;

    public static bool IsHeldItemAnimating => Panel?.IsHeldItemAnimating ?? false;

    public static bool AllowScroll
    {
        get => Panel?.AllowScroll ?? true;
        set
        {
            if (Panel != null) Panel.AllowScroll = value;
        }
    }

    public static bool IsItemModelVisible
    {
        get => Panel?.IsItemModelVisible ?? true;
        set
        {
            if (Panel != null) Panel.IsItemModelVisible = value;
        }
    }

    public static short SelectedSlotId
    {
        get => Panel?.SelectedSlotId ?? 0;
        set
        {
            if (Panel != null) Panel.SelectedSlotId = value;
        }
    }

    public static event Action<short> OnSlotChanged
    {
        add
        {
            if (Panel != null) Panel.OnSlotChanged += value;
        }
        remove
        {
            if (Panel != null) Panel.OnSlotChanged -= value;
        }
    }

    public static ItemSlot CurrentSlot() => Panel?.CurrentSlot();

    public static bool IsHolding<T>() where T : Item => Panel?.IsHolding<T>() ?? false;

    public static void SetSelectedSlot(short id) => Panel?.SetSelectedSlot(id);

    public static void ResetLastSelected() => Panel?.ResetLastSelected();

    public static void ApplyHotbarLayout() => Panel?.ApplyHotbarLayout();

    public static void SetFlashlight(LocalAstronaut astronaut) => Panel?.SetFlashlight(astronaut);

    public static void BlockHands() => Panel?.BlockHands();

    public static void UnblockHands() => Panel?.UnblockHands();

    public static void ResetHandsBlock() => Panel?.ResetHandsBlock();

    public static bool TryPlaceItem(out short id)
    {
        id = 0;
        return Panel != null && Panel.TryPlaceItem(out id);
    }
}
