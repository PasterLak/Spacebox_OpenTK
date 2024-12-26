
using DryIoc.ImTools;
using ImGuiNET;
using Spacebox.FPS;
using System;

namespace Spacebox.Common.GUI
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

            ImGui.TextColored(fpsColor, $"FPS: {fps}");
        }
    }
}
