using Engine.CanvasUI;
using Spacebox.Game.GUI.Sounds;

namespace Spacebox.Game.GUI.Canvas.Items;

public static class SlotDrag
{
    public static ItemSlot Source { get; private set; }
    public static bool IsActive => Source != null;

    public static void Begin(ItemSlot slot)
    {
        Source = slot != null && slot.HasItem ? slot : null;
        if (Source != null) UiSounds.Play(UiSound.SlotPick);
    }

    public static void Cancel()
    {
        Source = null;
    }

    public static void Release(CanvasRoot root)
    {
        var source = Source;
        Source = null;
        if (source == null || !source.HasItem || root == null) return;

        var hovered = root.HitTest(root.PointerPosition);
        var target = FindTarget(hovered);
        if (target != null)
        {
            target.AcceptDrop(source);
            SlotDropSounds.Play(target);
            return;
        }

        if (hovered == null) SlotTransfer.DropInWorld(source);
    }

    public static ISlotDropTarget FindTarget(Control control)
    {
        for (var cursor = control; cursor != null; cursor = cursor.Parent as Control)
        {
            if (cursor is ISlotDropTarget target) return target;
        }
        return null;
    }
}
