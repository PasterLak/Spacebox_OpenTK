using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
