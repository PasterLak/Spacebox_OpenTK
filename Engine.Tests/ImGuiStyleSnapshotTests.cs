using System.Numerics;
using Engine.GUI;
using ImGuiNET;

namespace Engine.Tests;

public sealed class ImGuiStyleSnapshotTests : IDisposable
{
    private readonly IntPtr _previous;
    private readonly IntPtr _context;

    public ImGuiStyleSnapshotTests()
    {
        _previous = ImGui.GetCurrentContext();
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
    }

    public void Dispose()
    {
        ImGui.DestroyContext(_context);
        ImGui.SetCurrentContext(_previous);
    }

    [Fact]
    public void Restore_UndoesColorsVariablesAndFontScale()
    {
        var style = ImGui.GetStyle();
        style.WindowPadding = new Vector2(3f, 4f);
        style.Colors[(int)ImGuiCol.Text] = new Vector4(1f, 0f, 0f, 1f);
        ImGui.GetIO().FontGlobalScale = 1f;

        var snapshot = ImGuiStyleSnapshot.Capture();

        ImGui.StyleColorsLight();
        style.WindowPadding = new Vector2(20f, 20f);
        style.FrameRounding = 9f;
        ImGui.GetIO().FontGlobalScale = 2f;

        snapshot.Restore();

        Assert.Equal(new Vector2(3f, 4f), ImGui.GetStyle().WindowPadding);
        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
        Assert.Equal(1f, ImGui.GetIO().FontGlobalScale);
    }
}
