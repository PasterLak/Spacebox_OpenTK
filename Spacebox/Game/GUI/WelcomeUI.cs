using ImGuiNET;
using Spacebox.Common;
using Spacebox.Common.Audio;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class WelcomeUI
    {
        protected static bool _isVisible = false;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                ToggleManager.Instance.SetState("player", !_isVisible);
                ToggleManager.Instance.SetState("mouse", _isVisible);
                ToggleManager.Instance.SetState("inventory", _isVisible);
                Settings.ShowInterface = !_isVisible;

                if (_isVisible) Input.ShowCursor();
                else Input.HideCursor();
            }
        }

        private static AudioSource clickAudio;
        private static IntPtr bannerTextureId;

        public static void Init()
        {
            clickAudio = new AudioSource(SoundManager.GetClip("click1"));
            var texture = TextureManager.GetTexture("Resources/Textures/UI/welcome.jpg");
            texture.FlipY();
            texture.UpdateTexture(true);
            if (texture != null) 
            bannerTextureId = texture.Handle;
        }

        public static void OnPlayerSpawned(bool savedState)
        {
     
            IsVisible = savedState;
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;

            if(Input.GetCursorState() != OpenTK.Windowing.Common.CursorState.Normal)
            {
                Input.ShowCursor();
            }

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            float windowWidth = displaySize.Y * 0.55f;
            float windowHeight = displaySize.Y * 0.7f;
            Vector2 windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));

            ImGui.Begin("Welcome!",
                ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse);

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            const string text = "Welcome to Spacebox!";
            var textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.502f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(0.1f, 0.1f, 0.1f, 0.5f), text);
            ImGui.SetCursorPos(new Vector2(windowWidth * 0.5f - textSize.X * 0.5f, textSize.Y));
            ImGui.TextColored(new Vector4(1.0f, 0.75f, 0.0f, 1f), text);


            float sidePadding = windowWidth * 0.05f;
            float topPadding = windowHeight * 0.10f;
            float blockVerticalSpacing = windowHeight * 0.025f;
            float imageHeight = windowHeight * 0.40f;
            ImGui.SetCursorPos(new Vector2(sidePadding, topPadding));

            if (bannerTextureId != IntPtr.Zero)
            {
                ImGui.Image(bannerTextureId, new Vector2(windowWidth - sidePadding * 2, imageHeight));
            }
            else
            {
                ImGui.BeginChild("BannerPlaceholder", new Vector2(windowWidth - sidePadding * 2, imageHeight));
                ImGui.Text("Banner Image Placeholder");
                ImGui.EndChild();
            }

            float nextYPos = topPadding + imageHeight + blockVerticalSpacing;
            ImGui.SetCursorPosY(nextYPos);
            float textBlockWidth = windowWidth - sidePadding * 2;
            ImGui.SetCursorPosX(sidePadding);

            ImGui.BeginChild("TextBlock", new Vector2(textBlockWidth, textBlockWidth/2f));
            ImGui.TextWrapped("Welcome to Spacebox! \n" +
                "Here you can display any information, such as tips or a game description.");
            ImGui.EndChild();

            float buttonWidth = windowWidth * 0.25f;
            float buttonHeight = sidePadding;
            float bottomPadding = windowHeight * 0.05f;
            float buttonY = windowHeight - bottomPadding - buttonHeight;
            float buttonX = (windowWidth - buttonWidth) * 0.5f;
            ImGui.SetCursorPos(new Vector2(buttonX, buttonY));

            GameMenu.CenterButtonWithBackground("Close", buttonWidth, buttonHeight, () =>
            {
                clickAudio?.Play();
                IsVisible = false;
            });

            ImGui.PopStyleColor(1);
            ImGui.End();
        }
    }
}
