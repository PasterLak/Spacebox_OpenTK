using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public readonly struct MenuStyle : IDisposable
{
    private const int PushedColors = 5;

    private readonly Vector2 _itemSpacing;
    private readonly Vector2 _windowPadding;

    private MenuStyle(Vector2 itemSpacing, Vector2 windowPadding)
    {
        _itemSpacing = itemSpacing;
        _windowPadding = windowPadding;
    }

    public static MenuStyle Begin()
    {
        var style = ImGui.GetStyle();
        var scope = new MenuStyle(style.ItemSpacing, style.WindowPadding);

        style.ItemSpacing = Vector2.Zero;
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1f, 0.75f, 0f, 1f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1f, 0.8f, 0f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(1f, 0.72f, 0f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Theme.Colors.Deep.ToUInt());
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
        return scope;
    }

    public void Dispose()
    {
        ImGui.PopStyleColor(PushedColors);
        var style = ImGui.GetStyle();
        style.ItemSpacing = _itemSpacing;
        style.WindowPadding = _windowPadding;
    }
}
