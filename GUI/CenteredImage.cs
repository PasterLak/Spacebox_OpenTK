using ImGuiNET;
using Spacebox.Extensions;
using System;
using System.Numerics;

namespace Spacebox.Common
{
    public static class CenteredImage
    {
        private static Texture2D _imageTexture;
        private static bool _isVisible = true;
        private static float _displayDuration = 0f;
        private static float _elapsedTime = 0f;
      

        private static float _parallaxIntensity = 0.01f; // Intensity of parallax effect

        public static void LoadImage(string path, bool pixelated = false)
        {
            if (_imageTexture != null)
            {
                _imageTexture.Dispose();
            }
            _imageTexture = new Texture2D(path, pixelated, false);
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
            if (!_isVisible || _imageTexture == null)
                return;

            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 displaySize = io.DisplaySize;

            DrawImage(displaySize, scale);
            DrawCenterText(displaySize);
            DrawGameVersion(displaySize);
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

            // Позиция изображения с учетом параллакс-эффекта
            Vector2 mousePosition = Input.Mouse.Position.ToSystemVector2();
            Vector2 offset = (mousePosition - displaySize / 2f) * _parallaxIntensity;

            float posX = (displaySize.X - imageWidth) / 2f + offset.X;
            float posY = (displaySize.Y - imageHeight) / 2f - imageHeight / 1.2f + offset.Y;

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(displaySize, ImGuiCond.Always);
            ImGui.Begin("CenteredImageWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus);
            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.Image((IntPtr)_imageTexture.Handle, new Vector2(imageWidth, imageHeight));
            ImGui.End();
        }

        private static void DrawCenterText(Vector2 displaySize)
        {
            float alpha = (float)(0.5 * (Math.Sin(_elapsedTime * 2.0f) + 1));
            Vector4 textColor = new Vector4(1, 1, 1, alpha);

            Vector2 textSize = ImGui.CalcTextSize("Press Enter to start");
            float textPosX = (displaySize.X - textSize.X) / 2f;
            float textPosY = displaySize.Y - displaySize.Y / 3f;

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.Begin("CenterTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.TextColored(textColor, "Press Enter to start");
            ImGui.End();
        }

        private static void DrawGameVersion(Vector2 displaySize)
        {
            Vector2 textSize = ImGui.CalcTextSize($"Version {Application.Version}    ") + new Vector2(5,0);
            float textPosX = 15f;
            float textPosY = displaySize.Y - textSize.Y - 20f;

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.Begin("VersionTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.Text($"Version {Application.Version}");
            ImGui.End();
        }

        public static void Dispose()
        {
            if (_imageTexture != null)
            {
                _imageTexture.Dispose();
                _imageTexture = null;
            }
        }
    }
}
