using OpenTK.Windowing.Common;

namespace Spacebox.Common
{
    public static class Time
    {
        public static FrameEventArgs Frame;
        public static float Delta => (float)Frame.Time;
        //public static float Total => (float)GameTime.TotalGameTime.TotalSeconds;

        public static void Update(FrameEventArgs frame)
        {
            Frame = frame;
        }

    }
}
