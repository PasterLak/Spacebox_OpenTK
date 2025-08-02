using ImGuiNET;
using OpenTK.Mathematics;
using Engine.Audio;

using NumVector4 = System.Numerics.Vector4;
using OpenTKVector4 = OpenTK.Mathematics.Vector4;

namespace Engine.GUI
{


    public class Overlay
    {
        private static bool _isVisible = false;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                Time.EnableProfiling = value;
            }
        }


        public static NumVector4 Red = new NumVector4(1f, 0f, 0f, 1f);
        public static NumVector4 Yellow = new NumVector4(1f, 1f, 0f, 1f);
        public static NumVector4 Green = new NumVector4(0f, 1f, 0f, 1f);
        public static NumVector4 Orange = new NumVector4(1f, 0.5f, 0f, 1f);

        private static readonly List<OverlayElement> OverlayElements = new List<OverlayElement>();

        private const float MemData = 1024.0f * 1024.0f;
        private static long memoryBytes;
        private static double memoryMB;

        static Overlay()
        {
            OverlayElements.Add(new FPSElement());
            OverlayElements.Add(new CameraElement());
            
            OverlayElements.Add(new GameLoopElement());
            OverlayElements.Add(new LightingElement());
            OverlayElements.Add(new ThreadsElement());
        }

        public static void AddElement(OverlayElement element)
        {
            if (element == null) return;

            OverlayElements.Add(element);
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new NumVector4(0, 0, 0, 0.8f));
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
            ImGui.SetWindowPos(new System.Numerics.Vector2(20, 40), ImGuiCond.Always);


            foreach (var e in OverlayElements)
            {
                e.BeforeDraw?.Invoke();
                e.ElementBefore?.OnGUIText();
                e.OnGUIText();
                e.ElementAfter?.OnGUIText();
                e.AfterDraw?.Invoke();
            }


            ImGui.End();
            ImGui.PopStyleColor();
        }

        public static OverlayElement? GetElementByType(Type type)
        {
            foreach (var e in OverlayElements)
            {
                if(e.GetType() == type) return e;
            }

            return null;
        }
    }
}
