﻿using OpenTK.Mathematics;
using ImGuiNET;
using Engine;
using Spacebox.Game;

namespace Spacebox.Game.GUI
{
    public static class TagManager
    {
        private static HashSet<Tag> _tags = new HashSet<Tag>();

        public static void RegisterTag(Tag tag)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
            else
            {
                Debug.Error("New Tag was not registered, this tag is already registered!");
            }
        }

        public static void UnregisterTag(Tag tag)
        {
            if (_tags.Contains(tag))
            {
                _tags.Remove(tag);
            }
        }

        public static void UnregisterTagByText(string text)
        {
            foreach (Tag tag in _tags)
            {
                if (tag.Text == text)
                {
                    _tags.Remove(tag);
                    return;
                }
            }
        }

        public static void ClearTags()
        {
            _tags.Clear();
        }

        public static void DrawTags(Camera camera, int screenWidth, int screenHeight)
        {   
            if (camera == null) return;
            if (_tags.Count == 0) return;
            if (!Settings.ShowInterface) return;

            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);

            ImGui.Begin("TagWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                                     ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus);

            foreach (var tag in _tags)
            {
                Vector2? screenPosNullable = camera.WorldToScreenPoint(tag.WorldPosition, screenWidth, screenHeight);
                if (screenPosNullable.HasValue)
                {
                    Vector2 screenPos = screenPosNullable.Value;
                    if (screenPos.X > 0 && screenPos.X <= screenWidth &&
                        screenPos.Y > 0 && screenPos.Y <= screenHeight)
                    {
                        var textSize = ImGui.CalcTextSize(tag.Text);
                        ImGui.SetCursorPos(tag.GetTextPosition(screenPos.ToSystemVector2(), textSize));
                        var drawList = ImGui.GetWindowDrawList();
                        float distance = (camera.Position - tag.WorldPosition).Length;
                        float newFontSize = Tag.CalculateFontSize(distance);
                        drawList.AddText(LoadFont(), newFontSize, ImGui.GetCursorPos(), tag.ColorUint, tag.Text);
                    }
                }
            }

            ImGui.End();
        }

        public static void OnResized(Vector2 screenSize)
        {
            Tag.SetFontSizes(screenSize);
        }

        private static ImFontPtr LoadFont()
        {
            var io = ImGui.GetIO();
            return io.FontDefault;
        }
    }
}
