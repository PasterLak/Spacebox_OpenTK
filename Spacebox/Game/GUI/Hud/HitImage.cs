
using Spacebox.Game;
using System.Numerics;
using Engine;
namespace Spacebox.Game.GUI
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

        public bool IsVisible => _isVisible;
        public float Opacity => _opacity;
        public Texture2D Texture => _imageTexture;

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
          
            _imageTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/hit.png");
            _imageTexture.FilterMode = FilterMode.Nearest;
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
                _opacity = MathF.Max(1f - _fadeElapsed / _fadeDuration, 0f);

                if (_fadeElapsed >= _fadeDuration)
                {
                    _isVisible = false;
                    _isFadingOut = false;
                }
            }
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
