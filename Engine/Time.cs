﻿using OpenTK.Windowing.Common;
using System.Diagnostics;

namespace Engine
{
    public static class Time
    {
        public static FrameEventArgs Frame;
        public static float Delta => (float)Frame.Time * TimeSize;
        public static float TimeSize = 1f;

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

        private static int _tickCount = 0;
        private static float _tickElapsedTime = 0.0f;
        private static float _targetTPS = 20.0f; 
        private static float _tickInterval = 1.0f / 20.0f;
        private static int _totalTicks = 0;
        private static float _tickAccumulatedTime = 0.0f;
        private static float _currentTPS;

        public static int TPS => (int)_currentTPS;
        public static int FPS => (int)_fps;
        public static double RenderTime => _renderTime;
        public static double AverageRenderTime => _averageRenderTime;
        public static byte RenderTimePercent = 0;

        public static double UpdateTime => _updateTime;
        public static double AverageUpdateTime => _averageUpdateTime;
        public static byte UpdateTimePercent = 0;

        public static double OnGUITime => _onGUITime;
        public static double AverageOnGUITime => _averageOnGUITime;

        public static byte OnGUITimePercent = 0;

        public static bool EnableProfiling { get; set; } = true;

        public static float TargetTPS
        {
            get => _targetTPS;
            set
            {
                if (value > 0)
                {
                    _targetTPS = value;
                    _tickInterval = 1.0f / _targetTPS;
                }
            }
        }

        public static event Action OnTick;

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

        private static void CalculatePercent()
        {
            var max = AverageRenderTime + AverageUpdateTime + AverageOnGUITime; // 50 8 5
            float onePercent = (float)(max / 100f);

            RenderTimePercent = (byte)(AverageRenderTime / onePercent);
            UpdateTimePercent = (byte)(AverageUpdateTime / onePercent);
            OnGUITimePercent = (byte)(AverageOnGUITime / onePercent);
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

                CalculatePercent();
            }
        }

        public static void HandleTicks()
        {
            _tickAccumulatedTime += Delta;

            while (_tickAccumulatedTime > _tickInterval)
            {
                OnTick?.Invoke();
                _tickAccumulatedTime -= _tickInterval;
                _totalTicks++;
                _tickCount++;
                _tickElapsedTime += _tickInterval;

            }

            if (_tickElapsedTime > 1.0)
            {
                _currentTPS = _tickCount / _tickElapsedTime;
                _tickCount = 0;
                _tickElapsedTime = 0.0f;
            }
        }
    }
}
