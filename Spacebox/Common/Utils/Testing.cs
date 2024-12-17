
using System.Diagnostics;

namespace Spacebox.Common.Utils
{
    public class Testing
    {
        private static Stopwatch _stopwatch = new Stopwatch();
        private static Mode _mode;

        public enum Mode
        {
            Detailed,
            Shortened
        }

        public static void Start(Mode mode = Mode.Detailed)
        {
            _mode = mode;
            Debug.WriteLine("Start Stopwatch");
            _stopwatch.Start();
        }

        public static void End()
        {
            _stopwatch.Stop();

            if (_mode == Mode.Detailed)
            {
                Debug.WriteLine("Stopwatch result: " + _stopwatch.Elapsed);
            }
            else
            {
                TimeSpan ts = _stopwatch.Elapsed;

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);

                Debug.WriteLine("Stopwatch result: " + elapsedTime + " h:m:s:ms");
            }

            _stopwatch.Reset();
        }
    }
}
