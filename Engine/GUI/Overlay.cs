using System;
using System.Collections.Generic;
using Engine.CanvasUI;
using Engine.UI;
using OpenTK.Mathematics;

namespace Engine.GUI
{


    public class Overlay
    {
        private const float RefreshInterval = 0.25f;

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

        public static Color4 Red = new Color4(1f, 0.3f, 0.3f, 1f);
        public static Color4 Yellow = new Color4(1f, 1f, 0.3f, 1f);
        public static Color4 Green = new Color4(0.35f, 1f, 0.35f, 1f);
        public static Color4 Orange = new Color4(1f, 0.6f, 0.2f, 1f);
        public static Color4 Blue = new Color4(0.45f, 0.6f, 1f, 1f);
        public static Color4 White = new Color4(1f, 1f, 1f, 1f);

        private static readonly List<OverlayElement> GlobalElements = new List<OverlayElement>();
        private static readonly List<OverlayElement> SceneElements = new List<OverlayElement>();
        private static readonly OverlayText Text = new OverlayText();

        private static DebugCanvas _canvas;
        private static float _refreshTimer;
        private static float _elapsed;

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

        public static void Update()
        {
            if (!_isVisible || Application.IsHeadless)
            {
                if (_canvas != null) _canvas.Layer.Visible = false;
                _refreshTimer = 0f;
                return;
            }

            if (_canvas == null)
            {
                _canvas = new DebugCanvas("Overlay");
                DebugCanvas.Area(_canvas.Root, UiLayer.Debug).AddChild(new OverlayPanel(Text));
            }

            _elapsed += Time.Delta;
            _refreshTimer -= Time.Delta;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = RefreshInterval;
                WriteTo(Text, _elapsed);
                _elapsed = 0f;
            }

            _canvas.Layer.Visible = true;
            _canvas.Layer.Update(Time.Delta);
        }

        public static void Draw()
        {
            _canvas?.Layer.Render();
        }

        public static void Shutdown()
        {
            _canvas?.Dispose();
            _canvas = null;
        }

        public static void WriteTo(OverlayText text, float elapsed)
        {
            text.Clear();
            text.Elapsed = elapsed;
            WriteElements(GlobalElements, text);
            WriteElements(SceneElements, text);
        }

        private static void WriteElements(List<OverlayElement> elements, OverlayText text)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                e.BeforeDraw?.Invoke();
                e.ElementBefore?.Write(text);
                e.Write(text);
                e.ElementAfter?.Write(text);
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
