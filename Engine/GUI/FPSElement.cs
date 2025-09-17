using ImGuiNET;

namespace Engine.GUI
{
    using NumVector4 = System.Numerics.Vector4;
    using OpenTKVector4 = OpenTK.Mathematics.Vector4;

    public class FPSElement : OverlayElement
    {
        public static NumVector4 Red = new NumVector4(1f, 0f, 0f, 1f);
        public static NumVector4 Yellow = new NumVector4(1f, 1f, 0f, 1f);
        public static NumVector4 Green = new NumVector4(0f, 1f, 0f, 1f);
        public static NumVector4 Orange = new NumVector4(1f, 0.5f, 0f, 1f);

        public override void OnGUIText()
        {
            float fps = Time.FPS;
            float tps = Time.TPS;
            NumVector4 fpsColor;

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

            ImGui.TextColored(fpsColor, $"FPS: {fps}" + t);
            ImGui.Text($"Limit FPS:");
            ImGui.SameLine();
            if (FrameLimiter.TargetFPS == 120)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "True");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "False");
            }
          

        }
    }
}
