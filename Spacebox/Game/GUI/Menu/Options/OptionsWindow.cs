using System.Numerics;
using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public class OptionsWindow : MenuWindow
    {
        private readonly IOptionsNavigator _options;

        public OptionsWindow(IOptionsNavigator options)
        {
            _options = options;
            Settings.LoadSettings();
        }

        public override void Render()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            float windowWidth = windowSize.X * 0.15f;
            float windowHeight = windowSize.Y * 0.3f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Options", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            float buttonWidth = windowWidth * 0.9f;
            float buttonHeight = windowHeight * 0.12f;
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.005f);

            const int buttonCount = 5;
            float spacing = (windowHeight - (buttonCount * buttonHeight)) / (buttonCount + 1);
            float currentY = spacing;

            currentY = Button("Audio", "Master, music, ambient, effects and interface volume", buttonWidth, buttonHeight, windowWidth, currentY, spacing, () => _options.Show<AudioWindow>());
            currentY = Button("Game", "Draw distance, mouse and camera settings", buttonWidth, buttonHeight, windowWidth, currentY, spacing, () => _options.Show<GameWindow>());
            currentY = Button("Graphics", "Display mode, frame rate, resolution and visual quality", buttonWidth, buttonHeight, windowWidth, currentY, spacing, () => _options.Show<GraphicsWindow>());
            currentY = Button("Controls", "Change key bindings", buttonWidth, buttonHeight, windowWidth, currentY, spacing, () => _options.Show<ControlsWindow>());
            Button("Back", "Return to the previous menu", buttonWidth, buttonHeight, windowWidth, currentY, spacing, _options.Close);

            ImGui.End();
        }

        private float Button(string label, string tooltip, float width, float height, float windowWidth, float y, float spacing, Action onClick)
        {
            ImGui.SetCursorPos(new Vector2((windowWidth - width) / 2, y));
            GameMenu.CenterButtonWithBackground(label, width, height, () =>
            {
                _options.PlayClick();
                onClick();
            });
            UIHelper.ShowTooltip(tooltip);
            return y + height + spacing;
        }
    }
}
