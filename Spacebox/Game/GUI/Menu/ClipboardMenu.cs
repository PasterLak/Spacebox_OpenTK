using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public static class ClipboardMenu
{
    public static bool ForTextField(string id, ref string value, int maxLength)
    {
        if (!ImGui.BeginPopupContextItem(id)) return false;

        bool changed = false;
        if (ImGui.MenuItem("Copy", "Ctrl+C", false, !string.IsNullOrEmpty(value))) ImGui.SetClipboardText(value);

        if (ImGui.MenuItem("Paste", "Ctrl+V"))
        {
            string pasted = ClipboardText.Clean(ImGui.GetClipboardText(), maxLength);
            if (pasted.Length > 0)
            {
                value = pasted;
                changed = true;
            }
        }

        if (ImGui.MenuItem("Clear", null, false, !string.IsNullOrEmpty(value)))
        {
            value = "";
            changed = true;
        }

        ImGui.EndPopup();
        return changed;
    }

    public static void ForCopy(string id, string label, string text)
    {
        if (!ImGui.BeginPopupContextItem(id)) return;

        if (ImGui.MenuItem(label, null, false, !string.IsNullOrEmpty(text))) ImGui.SetClipboardText(text);
        ImGui.EndPopup();
    }
}
