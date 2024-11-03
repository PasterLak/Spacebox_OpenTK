using ImGuiNET;
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

        public static void Draw(float scale = 0.5f)
        {
            if (!_isVisible || _imageTexture == null)
                return;

            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 displaySize = io.DisplaySize;
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

            float posX = (displaySize.X - imageWidth) / 2f;
            float posY = (displaySize.Y - imageHeight) / 2f - imageHeight / 1.2f;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(displaySize, ImGuiCond.Always);
            ImGui.Begin("CenteredImageWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus);
            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.Image((IntPtr)_imageTexture.Handle, new Vector2(imageWidth, imageHeight));

            // Плавное изменение прозрачности текста
            float alpha = (float)(0.5 * (Math.Sin(_elapsedTime * 2.0f) + 1)); // Альфа меняется от 0 до 1
            Vector4 textColor = new Vector4(1, 1, 1, alpha);

            Vector2 textSize = ImGui.CalcTextSize("Press Enter to start");
            float textPosX = (displaySize.X - textSize.X) / 2f;
            float textPosY = displaySize.Y - displaySize.Y / 3f;

            ImGui.SetCursorPos(new Vector2(textPosX, textPosY));
            ImGui.TextColored(textColor, "Press Enter to start");

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
