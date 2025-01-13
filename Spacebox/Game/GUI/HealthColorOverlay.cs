
using ImGuiNET;
using Spacebox.Common;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public static class HealthColorOverlay
    {

        private static bool _isEnabled = false;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private static bool _isActive = false;
        private static float _overlayAlpha = 0f;
        private static float _startAlpha = 0.5f;
        private static readonly float _fadeDuration = 1f;
        private static float _elapsedTime = 0f;

        private static Vector3 Color = new Vector3(0, 1, 0);

        public static void SetActive(Vector3 color, float startAlpha = 0.4f)
        {
            Color = color;
            _isActive = true;
            _overlayAlpha = startAlpha;
            _startAlpha = startAlpha;
            _elapsedTime = 0f;
            _isEnabled = true;
        }

        public static void Render()
        {
            if (_isEnabled && _isActive)
            {
                _elapsedTime += Time.Delta;
                float fadeProgress = _elapsedTime / _fadeDuration;

                if (fadeProgress >= 1f)
                {
                    _isActive = false;
                    _isEnabled = false;
                }
                else
                {
                    _overlayAlpha = OpenTK.Mathematics.MathHelper.Lerp(_startAlpha, 0f, fadeProgress);
                }

                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(Color.X, Color.Y, Color.Z, _overlayAlpha));
                ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y), ImGuiCond.Always);
                ImGui.Begin("Health Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs 
                    | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoTitleBar );
                ImGui.End();
                ImGui.PopStyleColor();
            }
        }
    }
}
