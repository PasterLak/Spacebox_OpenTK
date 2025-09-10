using ImGuiNET;
using System.Numerics;
using Engine;

namespace Spacebox.Game.GUI
{
    public enum FadeMode
    {
        None,
        FadeIn,
        FadeOut,
        FadeInOut
    }

    public static class ColorOverlay
    {
        private static bool _isEnabled = false;
        private static bool _isActive = false;
        private static float _currentAlpha = 0f;
        private static float _startAlpha = 0f;
        private static float _endAlpha = 0f;
        private static float _duration = 1f;
        private static float _elapsedTime = 0f;
        private static Vector3 _color = Vector3.One;
        private static FadeMode _fadeMode = FadeMode.None;
        private static Action _onComplete;
        private static Action<float> _onUpdate;
        private static float _holdDuration = 0f;
        private static float _holdElapsed = 0f;
        private static bool _isHolding = false;

        public static bool IsEnabled => _isEnabled;
        public static bool IsActive => _isActive;

        public static void StartFade(FadeMode mode, Vector3 color, float duration = 1f,
            float startAlpha = 0f, float endAlpha = 1f, float holdDuration = 0f, Action onComplete = null, Action<float> onUpdate = null)
        {
            if (mode == FadeMode.None) return;

            _fadeMode = mode;
            _color = color;
            _duration = duration;
            _holdDuration = holdDuration;
            _onComplete = onComplete;
            _onUpdate = onUpdate;
            _elapsedTime = 0f;
            _holdElapsed = 0f;
            _isHolding = false;
            _isActive = true;
            _isEnabled = true;

            switch (mode)
            {
                case FadeMode.FadeIn:
                    _startAlpha = startAlpha;
                    _endAlpha = endAlpha;
                    _currentAlpha = _startAlpha;
                    break;
                case FadeMode.FadeOut:
                    _startAlpha = endAlpha;
                    _endAlpha = startAlpha;
                    _currentAlpha = _startAlpha;
                    break;
                case FadeMode.FadeInOut:
                    _startAlpha = startAlpha;
                    _endAlpha = endAlpha;
                    _currentAlpha = _startAlpha;
                    break;
            }
        }

        public static void FadeOut(Vector3 color, float fromAlpha = 1f, float duration = 1f, Action onComplete = null, Action<float> onUpdate = null)
        {
            StartFade(FadeMode.FadeOut, color, duration, 0f, fromAlpha, 0f, onComplete, onUpdate);
        }

        public static void FadeIn(Vector3 color, float toAlpha = 1f, float duration = 1f, Action onComplete = null, Action<float> onUpdate = null)
        {
            StartFade(FadeMode.FadeIn, color, duration, 0f, toAlpha, 0f, onComplete, onUpdate);
        }
        public static void StartFade(FadeMode mode, OpenTK.Mathematics.Vector3 color, float duration = 1f,
            float startAlpha = 0f, float endAlpha = 1f, float holdDuration = 0f, Action onComplete = null, Action<float> onUpdate = null)
        {
            StartFade(mode, color.ToSystemVector3(), duration, startAlpha, endAlpha, holdDuration, onComplete, onUpdate);
        }

        public static void Stop()
        {
            _isActive = false;
            _isEnabled = false;
            _fadeMode = FadeMode.None;
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
            _onUpdate = null;
        }

        public static void OnGUI()
        {
            if (!_isEnabled || !_isActive) return;

           
            UpdateFade();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(_color.X, _color.Y, _color.Z, _currentAlpha));

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y), ImGuiCond.Always);

            ImGui.Begin("Color Overlay",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoTitleBar);

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private static void UpdateFade()
        {
            
            if (_isHolding)
            {
                _holdElapsed += Time.Delta;
                if (_holdElapsed >= _holdDuration)
                {
                    _isHolding = false;
                    _elapsedTime = 0f;
                    if (_fadeMode == FadeMode.FadeInOut)
                    {
                        float temp = _startAlpha;
                        _startAlpha = _endAlpha;
                        _endAlpha = temp;
                    }
                }
                return;
            }

            _elapsedTime += Time.Delta;
            float progress = Math.Min(_elapsedTime / _duration, 1f);
            _onUpdate?.Invoke(progress);
            switch (_fadeMode)
            {
                case FadeMode.FadeIn:
                    _currentAlpha = OpenTK.Mathematics.MathHelper.Lerp(_startAlpha, _endAlpha, progress);
                    if (progress >= 1f)
                    {
                        if (_holdDuration > 0f && !_isHolding)
                        {
                            _isHolding = true;
                            _holdElapsed = 0f;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    break;

                case FadeMode.FadeOut:
                    _currentAlpha = OpenTK.Mathematics.MathHelper.Lerp(_startAlpha, _endAlpha, progress);
                    if (progress >= 1f)
                    {
                        Stop();
                    }
                    break;

                case FadeMode.FadeInOut:
                    _currentAlpha = OpenTK.Mathematics.MathHelper.Lerp(_startAlpha, _endAlpha, progress);
                    if (progress >= 1f)
                    {
                        if (_startAlpha < _endAlpha)
                        {
                            if (_holdDuration > 0f && !_isHolding)
                            {
                                _isHolding = true;
                                _holdElapsed = 0f;
                            }
                            else
                            {
                                float temp = _startAlpha;
                                _startAlpha = _endAlpha;
                                _endAlpha = temp;
                                _elapsedTime = 0f;
                            }
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    break;
            }
        }

        public static void Flash(Vector3 color, float duration = 0.5f, float maxAlpha = 0.4f, Action onComplete = null)
        {
            StartFade(FadeMode.FadeInOut, color, duration * 0.5f, 0f, maxAlpha, 0f, onComplete);
        }

        public static void Flash(OpenTK.Mathematics.Vector3 color, float duration = 0.5f, float maxAlpha = 0.4f, Action onComplete = null)
        {
            Flash(color.ToSystemVector3(), duration, maxAlpha, onComplete);
        }
    }
}