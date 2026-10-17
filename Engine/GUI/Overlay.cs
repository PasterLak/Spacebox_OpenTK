using System;
using System.Collections.Generic;
using ImGuiNET;
using NumVector4 = System.Numerics.Vector4;

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
                Time.CalculateFPS = value;
            }
        }

        public static NumVector4 Red = new NumVector4(1f, 0f, 0f, 1f);
        public static NumVector4 Yellow = new NumVector4(1f, 1f, 0f, 1f);
        public static NumVector4 Green = new NumVector4(0f, 1f, 0f, 1f);
        public static NumVector4 Orange = new NumVector4(1f, 0.5f, 0f, 1f);

        private static readonly List<OverlayElement> GlobalElements = new List<OverlayElement>();
        private static readonly List<OverlayElement> SceneElements = new List<OverlayElement>();

        static Overlay()
        {
            AddGlobalElement(new FPSElement());
            AddGlobalElement(new CameraElement());
            AddGlobalElement(new GameLoopElement());
            AddGlobalElement(new LightingElement());
            AddGlobalElement(new ThreadsElement());
        }

        private static void AddGlobalElement(OverlayElement element)
        {
            if (element == null) return;
            GlobalElements.Add(element);
        }

        public static void AddElement(OverlayElement element)
        {
            if (element == null) return;
            SceneElements.Add(element);
        }

        public static void RemoveGlobalElement(Type type)
        {
            RemoveElementInternal(GlobalElements, type);
        }

        public static void RemoveElement(Type type)
        {
            RemoveElementInternal(SceneElements, type);
        }

        private static void RemoveElementInternal(List<OverlayElement> list, Type type)
        {
            if (type == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetType() == type)
                {
                    var removedElement = list[i];
                    list.RemoveAt(i);
                    CleanupReferences(removedElement);
                    return;
                }
            }
        }

        public static void ClearSceneElements()
        {
            foreach (var element in SceneElements)
            {
                CleanupReferences(element);
            }
            SceneElements.Clear();
        }

        private static void CleanupReferences(OverlayElement removedElement)
        {
            foreach (var element in GlobalElements)
            {
                if (element.ElementBefore == removedElement) element.ElementBefore = null;
                if (element.ElementAfter == removedElement) element.ElementAfter = null;
            }
            foreach (var element in SceneElements)
            {
                if (element.ElementBefore == removedElement) element.ElementBefore = null;
                if (element.ElementAfter == removedElement) element.ElementAfter = null;
            }
        }

        public static void OnGUI()
        {
            if (!_isVisible) return;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new NumVector4(0, 0, 0, 0.8f));
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);

            var screen = ImGui.GetIO().DisplaySize;
            ImGui.SetWindowPos(new System.Numerics.Vector2(screen.Y / 100f, screen.Y / 100f), ImGuiCond.Always);

            DrawElements(GlobalElements);
            DrawElements(SceneElements);

            ImGui.End();
            ImGui.PopStyleColor();
        }

        private static void DrawElements(List<OverlayElement> elements)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                e.BeforeDraw?.Invoke();
                e.ElementBefore?.OnGUIText();
                e.OnGUIText();
                e.ElementAfter?.OnGUIText();
                e.AfterDraw?.Invoke();
            }
        }

        public static OverlayElement? GetElementByType(Type type)
        {
            foreach (var e in GlobalElements)
            {
                if (e.GetType() == type) return e;
            }
            foreach (var e in SceneElements)
            {
                if (e.GetType() == type) return e;
            }
            return null;
        }
    }
}