
namespace Engine
{
    public static class CenteredImageMenu
    {
        public static bool ShowText = true;
        public static bool IsVisible => _isVisible;
        public static Texture2D Texture => _imageTexture;
        private static Texture2D _imageTexture;
        private static bool _isVisible = true;
        private static float _displayDuration = 0f;
        private static float _elapsedTime = 0f;
        private static float _parallaxIntensity = 0.01f;

        public static void LoadImage(string path, bool pixelated = false)
        {
            Resources.Release(_imageTexture);

            var texture2 = Resources.Load<Texture2D>(path);

            texture2.FilterMode = FilterMode.Nearest;
            texture2.FlipY();

             _imageTexture = texture2;
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

           // Debug.Log($"ImGui.Context: {ImGui.GetCurrentContext()}, IO.NativePtr: {ImGui.GetIO()}, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
           // Resources.PrintLoadedResources();

        public static void Dispose()
        {
            Resources.Release(_imageTexture);
            _imageTexture = null;
        }
    }
}
