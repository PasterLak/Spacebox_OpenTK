using ImGuiNET;

namespace Engine.GUI;

public sealed unsafe class ImGuiStyleSnapshot
{
    private ImGuiStyle _style;
    private float _fontGlobalScale;

    private ImGuiStyleSnapshot()
    {
    }

    public static ImGuiStyleSnapshot Capture()
    {
        var snapshot = new ImGuiStyleSnapshot
        {
            _style = *ImGui.GetStyle().NativePtr,
            _fontGlobalScale = ImGui.GetIO().FontGlobalScale
        };
        return snapshot;
    }

    public void Restore()
    {
        *ImGui.GetStyle().NativePtr = _style;
        ImGui.GetIO().FontGlobalScale = _fontGlobalScale;
    }
}
