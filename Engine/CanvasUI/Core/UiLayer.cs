namespace Engine.CanvasUI;

public enum UiLayer
{
    Background = 0,
    Hud = 100,
    Windows = 200,
    Popup = 300,
    Modal = 400,
    ModalPopup = 450,
    Tooltip = 500,
    Debug = 600
}

public sealed class UiLayerRoot : Control
{
    public UiLayer Layer { get; }

    public UiLayerRoot(UiLayer layer)
    {
        Layer = layer;
        Name = layer.ToString();
        MouseFilter = MouseFilter.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public bool HasVisibleContent
    {
        get
        {
            var children = Children;
            for (int i = 0; i < children.Count; i++)
                if (children[i].Visible) return true;
            return false;
        }
    }

    public static UiLayer? LayerOf(CanvasItem item)
    {
        for (var cursor = item; cursor != null; cursor = cursor.Parent)
            if (cursor is UiLayerRoot root) return root.Layer;
        return null;
    }
}
