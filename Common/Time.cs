// Time.cs
using OpenTK.Windowing.Common;
using System.Diagnostics;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public static class Time
    {
        public static FrameEventArgs Frame;
        public static float Delta => (float)Frame.Time;

        private static int _frameCount = 0;
        private static double _elapsedTime = 0.0;
        private static double _fps = 0.0;

        private static Stopwatch _renderStopwatch = new Stopwatch();
        private static double _renderTime = 0.0;
        private static double _totalRenderTime = 0.0;
        private static int _renderFrameCount = 0;
        private static double _averageRenderTime = 0.0;

        private static Stopwatch _updateStopwatch = new Stopwatch();
        private static double _updateTime = 0.0;
        private static double _totalUpdateTime = 0.0;
        private static int _updateFrameCount = 0;
        private static double _averageUpdateTime = 0.0;

        private static Stopwatch _onGUIStopwatch = new Stopwatch();
        private static double _onGUITime = 0.0;
        private static double _totalOnGUITime = 0.0;
        private static int _onGUiframeCount = 0;
        private static double _averageOnGUITime = 0.0;

        public static int FPS => (int)_fps;
        public static double RenderTime => _renderTime;
        public static double AverageRenderTime => _averageRenderTime;
        public static double UpdateTime => _updateTime;
        public static double AverageUpdateTime => _averageUpdateTime;
        public static double OnGUITime => _onGUITime;
        public static double AverageOnGUITime => _averageOnGUITime;

        public static bool EnableProfiling { get; set; } = true;

        public static void Update(FrameEventArgs frame)
        {
            Frame = frame;
            UpdateFPS(frame);
        }

        private static void UpdateFPS(FrameEventArgs e)
        {
            _frameCount++;
            _elapsedTime += e.Time;
            if (_elapsedTime >= 1.0)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0.0;
            }
        }

        public static void StartRender()
        {
            if (EnableProfiling)
                _renderStopwatch.Restart();
        }

        public static void EndRender()
        {
            if (EnableProfiling)
            {
                _renderStopwatch.Stop();
                _renderTime = _renderStopwatch.Elapsed.TotalMilliseconds;
                _totalRenderTime += _renderTime;
                _renderFrameCount++;
                if (_renderFrameCount >= 100)
                {
                    _averageRenderTime = _totalRenderTime / _renderFrameCount;
                    _totalRenderTime = 0.0;
                    _renderFrameCount = 0;
                }
            }
        }

        public static void StartUpdate()
        {
            if (EnableProfiling)
                _updateStopwatch.Restart();
        }

        public static void EndUpdate()
        {
            if (EnableProfiling)
            {
                _updateStopwatch.Stop();
                _updateTime = _updateStopwatch.Elapsed.TotalMilliseconds;
                _totalUpdateTime += _updateTime;
                _updateFrameCount++;
                if (_updateFrameCount >= 100)
                {
                    _averageUpdateTime = _totalUpdateTime / _updateFrameCount;
                    _totalUpdateTime = 0.0;
                    _updateFrameCount = 0;
                }
            }
        }

        public static void StartOnGUI()
        {
            if (EnableProfiling)
                _onGUIStopwatch.Restart();
        }

        public static void EndOnGUI()
        {
            if (EnableProfiling)
            {
                _onGUIStopwatch.Stop();
                _onGUITime = _onGUIStopwatch.Elapsed.TotalMilliseconds;
                _totalOnGUITime += _onGUITime;
                _onGUiframeCount++;
                if (_onGUiframeCount >= 100)
                {
                    _averageOnGUITime = _totalOnGUITime / _onGUiframeCount;
                    _totalOnGUITime = 0.0;
                    _onGUiframeCount = 0;
                }
            }
        }
    }
}
