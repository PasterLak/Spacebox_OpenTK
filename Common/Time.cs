using OpenTK.Windowing.Common;
using System;

namespace Spacebox.Common
{
    public static class Time
    {
        public static FrameEventArgs Frame;
        public static float Delta => (float)Frame.Time;

        // FPS calculation variables
        private static int _frameCount = 0;
        private static double _elapsedTime = 0.0;
        private static double _fps = 0.0;

        // Public property to get the current FPS value
        public static int FPS => (int)_fps;

        public static void Update(FrameEventArgs frame)
        {
            Frame = frame;
            UpdateFPS(frame);
        }

        private static void UpdateFPS(FrameEventArgs e)
        {
            _frameCount++;
            _elapsedTime += e.Time;

            // Update FPS every second
            if (_elapsedTime >= 1.0)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0.0;
            }
        }
    }
}
