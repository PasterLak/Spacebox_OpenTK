using OpenTK.Mathematics;

namespace Engine.GUI
{
    public class FPSElement : OverlayElement
    {
        public static Color4 Red = new Color4(1f, 0.3f, 0.3f, 1f);
        public static Color4 Yellow = new Color4(1f, 1f, 0.3f, 1f);
        public static Color4 Green = new Color4(0.35f, 1f, 0.35f, 1f);
        public static Color4 Orange = new Color4(1f, 0.6f, 0.2f, 1f);

        public override void Write(OverlayText text)
        {
            float fps = Time.FPS;
            float tps = Time.TPS;
            Color4 fpsColor;

            if (fps < 20f)
            {
                fpsColor = Red;
            }
            else if (fps < 40f)
            {
                fpsColor = Orange;
            }
            else if (fps < 60f)
            {
                fpsColor = Yellow;
            }
            else
            {
                fpsColor = Green;
            }

            string t = tps > 0 ? $", TPS: {tps}" : $"";

            text.Line($"FPS: {fps}" + t, fpsColor);
            text.Line("Limit FPS:");
            if (FrameLimiter.TargetFPS == 120)
            {
                text.Append("True", Green);
            }
            else
            {
                text.Append("False", Red);
            }


        }
    }
}
