using ImGuiNET;
using Spacebox.Extensions;
using Spacebox.Game;
using System;
using System.Numerics;

namespace Spacebox.Common
{
    public class HitImage : IDisposable
    {
        private Texture2D _imageTexture;
        private bool _isVisible = false;
        private float _displayDuration = 2f;
        private float _elapsedTime = 0f;
        private float _opacity = 1f;
        private bool _isFadingOut = false;
        private float _fadeDuration = 0.8f;
        private float _fadeElapsed = 0f;

        private const float scale = 0.1f;

        public HitImage()
        {
            LoadImage();
        }

        public void LoadImage()
        {
            if (_imageTexture != null)
            {
                _imageTexture.Dispose();
            }
            _imageTexture = new Texture2D("Resources/Textures/hit.png", true, false);
        }

        public void Show()
        {
            _isVisible = true;
        
            _elapsedTime = 0f;
            _opacity = 1f;
            _isFadingOut = false;
            _fadeElapsed = 0f;
        }

        public void Hide()
        {
            if (_isVisible && !_isFadingOut)
            {
                _isFadingOut = true;
                _fadeElapsed = 0f;
            }
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Update()
        {
            if (!_isVisible) return;

            _elapsedTime += Time.Delta;

            if (!_isFadingOut && _displayDuration > 0f && _elapsedTime >= _displayDuration)
            {
                Hide();
            }

            if (_isFadingOut)
            {
                _fadeElapsed += Time.Delta;
                _opacity = MathF.Max(1f - (_fadeElapsed / _fadeDuration), 0f);

                if (_fadeElapsed >= _fadeDuration)
                {
                    _isVisible = false;
                    _isFadingOut = false;
                }
            }
        }

        public void Draw()
        {
            if (!_isVisible) return;
            if (!Settings.ShowInterface) return;
            if (_imageTexture == null)
                return;

            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 displaySize = new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y);

            DrawImage(displaySize);
        }

        private void DrawImage(Vector2 displaySize)
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

            float posX = (displaySize.X - imageWidth) / 2f;
            float posY = (displaySize.Y - imageHeight) / 2f;

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(displaySize, ImGuiCond.Always);
            ImGui.Begin("CenteredImageWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus);

            Vector4 tintColor = new Vector4(1f, 1f, 1f, _opacity);
            ImGui.SetCursorPos(new Vector2(posX, posY));
            ImGui.Image((IntPtr)_imageTexture.Handle, new Vector2(imageWidth, imageHeight), Vector2.Zero, Vector2.One, tintColor, Vector4.Zero);
            ImGui.End();
        }

        public void Dispose()
        {
            if (_imageTexture != null)
            {
                _imageTexture.Dispose();
                _imageTexture = null;
            }
        }
    }
}
