using ImGuiNET;
using System.Numerics;


namespace Spacebox.Game.GUI.Menu;

public static class SettingsUI
{
    public static void Render(string windowId, string header, int buttonCount,
        Action<Vector2, float> drawContent, Action onSave, Action onBack)
    {
        var io = ImGui.GetIO();
        float ww = io.DisplaySize.X * 0.4f;
        float wh = io.DisplaySize.Y * 0.5f;
        var pos = GameMenu.CenterNextWindow2(ww, wh);

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(new Vector2(ww, wh));
        ImGui.Begin(windowId, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

        GameMenu.DrawElementColors(pos, new Vector2(ww, wh), io.DisplaySize.Y, 0.005f);

        float btnW = ww * 0.9f;
        float btnH = wh * 0.08f;
        float spacing = wh * 0.02f;

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing));
        var textSize = ImGui.CalcTextSize(header);
        ImGui.SetCursorPos(new Vector2((ww - textSize.X) / 2f, spacing));
        ImGui.Text(header);

        float headerBlockH = ImGui.GetTextLineHeightWithSpacing() * 1.5f;
        var listSize = new Vector2(btnW, wh - btnH * 1.5f - spacing * 4 - headerBlockH);

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing * 2 + headerBlockH));
        ImGui.BeginChild($"list##{windowId}", listSize);

        float rowH = 30f;
        drawContent(listSize, rowH);

        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, wh - btnH - spacing));

        if (onSave != null)
            ButtonWithBackground("Save", new Vector2(listSize.X / 2f - spacing, btnH),
                new Vector2((ww - btnW) / 2f, wh - btnH - spacing), onSave);

        if (onBack != null)
            ButtonWithBackground("Back", new Vector2(listSize.X / 2f - spacing, btnH),
            new Vector2((ww - btnW) / 2f + listSize.X / 2f + spacing, wh - btnH - spacing), onBack);

        ImGui.End();
    }

    public static void ButtonWithBackground(string label, Vector2 size, Vector2 cursorPos, Action onClick)
    {
        ImGui.SetCursorPos(cursorPos);
        Vector2 buttonPos = ImGui.GetCursorScreenPos();
        float offsetValue = size.Y * 0.1f;
        Vector2 offset = new Vector2(offsetValue, offsetValue);
        uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
        uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(buttonPos - offset, buttonPos + size + offset, borderColor);
        drawList.AddRectFilled(buttonPos, buttonPos + size + offset, lightColor);
        if (ImGui.Button(label, size))
        {

            onClick?.Invoke();
        }
    }
}
