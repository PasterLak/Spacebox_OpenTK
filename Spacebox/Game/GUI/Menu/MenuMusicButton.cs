using System.Numerics;
using Engine;
using Engine.Audio;
using ImGuiNET;

namespace Spacebox.Game.GUI.Menu;

public class MenuMusicButton
{
    private const string MusicOnIcon = "musicon";
    private const string MusicOffIcon = "musicoff";

    private readonly MenuMusicController _controller;
    private readonly AudioSource _clickSound;

    public MenuMusicButton(MenuMusicController controller, AudioSource clickSound)
    {
        _controller = controller;
        _clickSound = clickSound;
    }

    public void Draw()
    {
        _controller.Sync();

        Vector2 screenSize = ImGui.GetIO().DisplaySize;
        float buttonSize = screenSize.Y * 0.05f;
        float iconSize = buttonSize * 0.8f;
        float iconOffset = (buttonSize - iconSize) / 2f;

        ImGui.SetNextWindowPos(new Vector2(screenSize.X - buttonSize, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(buttonSize, buttonSize), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("MenuMusicButton", ImGuiWindowFlags.NoTitleBar |
                                       ImGuiWindowFlags.NoResize |
                                       ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoScrollbar |
                                       ImGuiWindowFlags.NoScrollWithMouse |
                                       ImGuiWindowFlags.NoBackground);

        Vector2 buttonPos = ImGui.GetCursorScreenPos();
        if (ImGui.InvisibleButton("##toggleMenuMusic", new Vector2(buttonSize, buttonSize)))
        {
            _clickSound?.Play();
            _controller.Toggle();
        }

        UIHelper.ShowTooltip(_controller.IsEnabled ? "Menu music: on" : "Menu music: off");

        Vector2 iconMin = new Vector2(buttonPos.X + iconOffset, buttonPos.Y + iconOffset);
        Vector2 iconMax = new Vector2(iconMin.X + iconSize, iconMin.Y + iconSize);
        uint tint = ImGui.IsItemHovered() ? 0xFFFFFFFF : 0xC8FFFFFF;
        UIAtlas.Get(_controller.IsEnabled ? MusicOnIcon : MusicOffIcon).Draw(ImGui.GetWindowDrawList(), iconMin, iconMax, tint);

        ImGui.End();
        ImGui.PopStyleVar();
    }
}
