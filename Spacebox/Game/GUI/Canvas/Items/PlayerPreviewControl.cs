using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class PlayerPreviewControl : TextureHandleRect, ISlotDropTarget
{
    private readonly Func<PlayerPreview> _preview;
    private readonly Action<ItemSlot> _onDrop;
    private bool _rotating;
    private Vector2 _last;

    public PlayerPreviewControl(Func<PlayerPreview> preview, Action<ItemSlot> onDrop)
    {
        _preview = preview;
        _onDrop = onDrop;
        Handle = () => _preview()?.TextureHandle ?? 0;
        MouseFilter = MouseFilter.Stop;
        TooltipText = "Drag to rotate, double-click to reset\nDrop equipment here to wear it";
    }

    public override bool IsInteractive => true;

    public Vector2 PixelSize => Root == null ? Size : Size * Root.Scale;

    public void AcceptDrop(ItemSlot source) => _onDrop?.Invoke(source);

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        if (Root != null && Root.ClickCount >= 2)
        {
            _preview()?.ResetView();
            return true;
        }

        _rotating = true;
        _last = localPosition;
        return true;
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        base.OnMouseMove(localPosition, pressed);
        if (!_rotating || !pressed) return;
        float scale = Root?.Scale ?? 1f;
        _preview()?.Rotate((localPosition - _last) * scale);
        _last = localPosition;
    }

    protected override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button != MouseButton.Left) return false;
        _rotating = false;
        return true;
    }
}
