using ImGuiNET;
using Spacebox.Engine.Extensions;
using System.Numerics;

namespace Spacebox.Engine
{
    public static class CenteredImageMenu
    {
        public static bool ShowText = true;
        private static Texture2D _imageTexture;
        private static bool _isVisible = true;
        private static float _displayDuration = 0f;
        private static float _elapsedTime = 0f;
        private static float _parallaxIntensity = 0.01f;

        public static void LoadImage(string path, bool pixelated = false)
        {
            _imageTexture?.Dispose();
            _imageTexture = TextureManager.GetTexture(path, pixelated, false);
        }

        public static void Show(float duration = 0f)
        {
            _isVisible = true;
            _displayDuration = duration;
            _elapsedTime = 0f;
        }

        public static void Hide()
        {
            _isVisible = false;
        }

        public static void Toggle()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                _elapsedTime = 0f;
            }
        }

        public static void Update()
        {
            if (!ShowText) return;

            _elapsedTime += Time.Delta;
            if (_isVisible && _displayDuration > 0f && _elapsedTime >= _displayDuration)
            {
                Hide();
            }
        }

        public static void SetParallaxIntensity(float intensity)
        {
            _parallaxIntensity = intensity;
        }

        public static void Draw(float scale = 0.5f)
        {
            if (_imageTexture == null || !_isVisible)
                return;

            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 displaySize = io.DisplaySize;

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(displaySize, ImGuiCond.Always);
            ImGui.Begin("OverlayWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs |
                                         ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
                                         ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus);

            DrawImage(displaySize, scale);
            DrawCenterText(displaySize);
            DrawGameVersion(displaySize);
            DrawAuthor(displaySize);

            ImGui.End();
        }

        private static void DrawImage(Vector2 displaySize, float scale)
        {
            float maxWidth = displaySize.X * scale;
            float maxHeight = displaySize.Y * scale;
            float imageAspect = (float)_imageTexture.Width / _imageTexture.Height;
            float imageWidth, imageHeight;

            if (maxWidth / imageAspect <= maxHeight)
            {
                imageWidth = maxWidth;
                imageHeight = maxWidth / imageAspect;
            }
            else
            {
                imageHeight = maxHeight;
                imageWidth = maxHeight * imageAspect;
            }

            Vector2 mousePosition = Input.Mouse.Position.ToSystemVector2();
            Vector2 offset = (mousePosition - displaySize / 2f) * _parallaxIntensity;

            float posX = (displaySize.X - imageWidth) / 2f + offset.X;
            float posY = (displaySize.Y - imageHeight) / 2f - imageHeight / 1.2f + offset.Y;

            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.Image((IntPtr)_imageTexture.Handle, new Vector2(imageWidth, imageHeight));
        }

        private static void DrawCenterText(Vector2 displaySize)
        {
            if (!ShowText) return;
            float alpha = 0.5f * (float)(Math.Sin(_elapsedTime * 2.0f) + 1);
            Vector4 textColor = new Vector4(1, 1, 1, alpha);

            string centerText = "Press Enter to start";
            Vector2 textSize = ImGui.CalcTextSize(centerText);
            float padding = 20f;

            float textPosX = (displaySize.X - textSize.X) / 2f;
            float textPosY = displaySize.Y - displaySize.Y / 3f;
            float maxTextPosY = displaySize.Y - textSize.Y - padding;
            textPosY = Math.Min(textPosY, maxTextPosY);
            textPosY = Math.Max(textPosY, padding);

            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.TextColored(textColor, centerText);
        }

        private static void DrawGameVersion(Vector2 displaySize)
        {
            string versionText = $"Version {Application.Version} alpha";
            Vector2 textSize = ImGui.CalcTextSize(versionText);
            float padding = 20f;

            float textPosX = padding;
            float textPosY = displaySize.Y - textSize.Y - padding;
            textPosY = Math.Max(textPosY, padding);

            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.Text(versionText);
        }

        private static void DrawAuthor(Vector2 displaySize)
        {
            string author = "Made by PasterLak";
            Vector2 textSize = ImGui.CalcTextSize(author);
            float padding = 20f;

            float textPosX = displaySize.X - textSize.X - padding;
            float textPosY = displaySize.Y - textSize.Y - padding;

            textPosX = Math.Max(textPosX, padding);
            textPosY = Math.Max(textPosY, padding);

            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.Text(author);
        }


        public static void Dispose()
        {
            _imageTexture?.Dispose();
            _imageTexture = null;
        }
    }
}
