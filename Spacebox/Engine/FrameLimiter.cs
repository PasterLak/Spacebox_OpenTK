using OpenTK.Windowing.Common;
using System.Diagnostics;
using System.Threading;

namespace Spacebox.Engine
{
    public static class FrameLimiter
    {
        public static bool IsRunning { get;  set; } = true;


        private static int _targetFPS = 120;
        public static int TargetFPS
        {
            get
            {
                return _targetFPS;
            }
            set
            {
                _targetFPS = value;
                _stopwatch.Stop();

                _targetFrameTime = 1.0 / _targetFPS;
                _stopwatch.Start();
            }
        }

        private static Stopwatch _stopwatch = new Stopwatch();
        private static double _targetFrameTime; // sec
        private static double _accumulator = 0.0;

        public static void Initialize(int targetFPS)
        {
            _targetFrameTime = 1.0 / targetFPS;
            _stopwatch.Start();
        }

        public static void SetUnlimited()
        {
            _stopwatch.Stop();

            _targetFPS = 9999;
            _targetFrameTime = 1.0 / 9999;
            _stopwatch.Start();
        }

        public static void Update()
        {
            if(!IsRunning)  return; 

            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            double deltaTime = currentTime - _accumulator;

            if (deltaTime < _targetFrameTime)
            {
                int sleepTime = (int)((_targetFrameTime - deltaTime) * 1000);
                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
            }

            _accumulator = _stopwatch.Elapsed.TotalSeconds;
        }
    }
}
