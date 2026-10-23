using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Items;

public static class SlotDrag
{
    public static ItemSlot Source { get; private set; }
    public static ItemSlot PendingImGuiDrop { get; private set; }
    public static bool IsActive => Source != null;
    public static Func<bool> IsPointerOverOtherUi { get; set; } = () => false;

    public static void Begin(ItemSlot slot)
    {
        Source = slot != null && slot.HasItem ? slot : null;
    }

    public static void Cancel()
    {
        Source = null;
        PendingImGuiDrop = null;
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
            return;
        }

        if (IsPointerOverOtherUi())
        {
            PendingImGuiDrop = source;
            return;
        }

        if (hovered == null) SlotTransfer.DropInWorld(source);
    }

    public static ItemSlot TakePendingImGuiDrop()
    {
        var slot = PendingImGuiDrop;
        PendingImGuiDrop = null;
        return slot;
    }

    public static void BeginFrame()
    {
        PendingImGuiDrop = null;
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
