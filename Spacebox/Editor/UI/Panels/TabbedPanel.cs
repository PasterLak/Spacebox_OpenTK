using ImGuiNET;

namespace Spacebox.Editor.UI.Panels;

public sealed class TabbedPanel : IEditorPanel
{
    private readonly IEditorPanel[] _tabs;

    public string Title { get; }

    public TabbedPanel(string title, params IEditorPanel[] tabs)
    {
        Title = title;
        _tabs = tabs;
    }

    public void Draw(EditorContext context)
    {
        if (!ImGui.BeginTabBar("##" + Title)) return;

        for (int i = 0; i < _tabs.Length; i++)
        {
            if (!ImGui.BeginTabItem(_tabs[i].Title)) continue;
            _tabs[i].Draw(context);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}
