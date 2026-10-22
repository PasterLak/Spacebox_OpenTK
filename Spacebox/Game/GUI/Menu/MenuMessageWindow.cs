using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public sealed class MenuMessageWindow
{
    private static readonly Vector4 TitleColor = new Vector4(1f, 0.45f, 0.35f, 1f);

    private string _title;
    private string _text;

    public bool IsOpen => _text != null;

    public void Show(string title, string text)
    {
        _title = title ?? "";
        _text = text ?? "";
    }

    public void Close()
    {
        _title = null;
        _text = null;
    }

    public void Render(GameMenu menu)
    {
        if (!IsOpen) return;

        Vector2 display = ImGui.GetIO().DisplaySize;
        float width = MathF.Max(display.Y * 0.7f, 420f);
        float height = display.Y * 0.36f;
        Vector2 position = GameMenu.CenterNextWindow2(width, height);
        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowFocus();
        ImGui.Begin("##menuMessage", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y, 0.005f);

        float padding = height * 0.06f;
        float buttonHeight = height * 0.14f;
        float buttonWidth = width * 0.3f;

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _title);
        ImGui.SetCursorPosX(padding);
        if (ImGui.BeginChild("##messageText", new Vector2(width - padding * 2f, height - padding * 3f - buttonHeight - ImGui.GetTextLineHeightWithSpacing())))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(_text);
            ImGui.PopTextWrapPos();
        }
        ImGui.EndChild();

        menu.ButtonWithBackground("OK", new Vector2(buttonWidth, buttonHeight), new Vector2((width - buttonWidth) * 0.5f, height - padding - buttonHeight), Close);
        ImGui.End();
    }
}
