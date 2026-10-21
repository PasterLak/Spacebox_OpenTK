using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public sealed class UnsavedChangesPrompt
{
    private const string PopupId = "Unsaved changes##options";
    private const string Question = "Save changes?";

    private bool _openRequested;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        _openRequested = true;
        IsOpen = true;
    }

    public void Render(Action onSave, Action onDiscard, Action onCancel)
    {
        if (!IsOpen) return;
        if (_openRequested)
        {
            ImGui.OpenPopup(PopupId);
            _openRequested = false;
        }

        Vector2 displaySize = ImGui.GetIO().DisplaySize;
        float windowWidth = displaySize.Y * 0.22f;
        float windowHeight = displaySize.Y * 0.16f;
        Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);

        if (!ImGui.BeginPopupModal(PopupId, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar))
        {
            IsOpen = false;
            return;
        }

        float buttonWidth = windowWidth * 0.8f;
        float buttonHeight = windowHeight * 0.18f;
        float spacing = windowHeight * 0.035f;
        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

        var textSize = ImGui.CalcTextSize(Question);
        ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, spacing * 2f));
        ImGui.Text(Question);
        ImGui.Dummy(new Vector2(0, spacing));

        Button("Save", buttonWidth, buttonHeight, onSave);
        ImGui.Dummy(new Vector2(0, spacing));
        Button("Don't Save", buttonWidth, buttonHeight, onDiscard);
        ImGui.Dummy(new Vector2(0, spacing));
        Button("Cancel", buttonWidth, buttonHeight, onCancel);

        ImGui.EndPopup();
    }

    private void Button(string label, float width, float height, Action onClick)
    {
        GameMenu.CenterButtonWithBackground(label, width, height, () =>
        {
            IsOpen = false;
            ImGui.CloseCurrentPopup();
            onClick();
        });
    }
}
