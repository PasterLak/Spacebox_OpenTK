using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Engine.GUI;
using Engine.Light;
using Engine.SceneManagement;
using ImGuiNET;

namespace Engine.UI
{
    public static class DevGui
    {
        private static Type _selectedSceneType;
        private static Guid? _selectedNodeId;

        public static void Draw()
        {
            if (!Overlay.IsVisible) return;

            var io = ImGui.GetIO();
            float width = io.DisplaySize.X * 0.25f;
            float height = io.DisplaySize.Y;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.1f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0.5f));

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.25f, 0.6f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.25f, 0.25f, 0.25f, 0.65f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 0.7f));

            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.3f, 0.3f, 0.3f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.4f, 0.4f, 0.4f, 0.8f));

            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - width, 0));
            ImGui.SetNextWindowSize(new Vector2(width, height));
            ImGui.Begin("Developer Panel", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

            if (ImGui.Button("Reload Scene")) SceneManager.Reload();
            ImGui.SameLine();
            if (ImGui.Button("Back")) SceneManager.LoadPrevious();

            ImGui.Separator();
            ImGui.Text("Registered Scenes:");

            float lineH = ImGui.GetTextLineHeightWithSpacing();
            ImGui.BeginChild(
                "##scene_list",
                new Vector2(0, lineH * 5)
               // ImGuiWindowFlags.Border
            );
            foreach (var type in GetRegisteredScenes().Where(t => t != typeof(ErrorScene)))
            {
                bool selected = _selectedSceneType == type;

                var name = type.Name.Replace("Scene", "");
                if (ImGui.Selectable(name, selected))
                    _selectedSceneType = type;
                if (selected && ImGui.IsItemClicked())
                    SceneManager.Load(type);
            }
            ImGui.EndChild();

            ImGui.Spacing();
            ImGui.Text("Lighting:");
            var ambient = Lighting.AmbientColor.ToSystemVector3();
            if (ImGui.ColorEdit3("Ambient", ref ambient))
                Lighting.AmbientColor = ambient.ToOpenTKVector3();
            var fogCol = Lighting.FogColor.ToSystemVector3();
            if (ImGui.ColorEdit3("Fog Color", ref fogCol))
                Lighting.FogColor = fogCol.ToOpenTKVector3();

            int fogDenSlider = (int)(Lighting.FogDensity * 10000);
  
            if (ImGui.SliderInt("Fog Density", ref fogDenSlider, 0, 100))
            {

                Lighting.FogDensity = fogDenSlider;
            }


            ImGui.Separator();
            ImGui.Text($"Hierarchy:");

            ImGui.BeginChild(
                "##scene_tree",
                new Vector2(0, height * 0.4f)
                //ImGuiWindowFlags.
            );
            DrawNodeTree(SceneManager.Current);
            ImGui.EndChild();

            ImGui.Separator();
            if (_selectedNodeId.HasValue)
            {
                var node = FindNodeById(SceneManager.Current, _selectedNodeId.Value);
                if (node != null)
                {
                    ImGui.Text($"Node: {node.Name}");
                    ImGui.Text("Components:");
                    foreach (var c in node.Components)
                        ImGui.BulletText(c.GetType().Name);
                }
            }

            ImGui.End();
            ImGui.PopStyleColor(8);
        }

        private static void DrawNodeTree(Node3D node)
        {
            ImGui.PushID(node.Id.ToString());

            bool leaf = node.Children.Count == 0;
            ImGuiTreeNodeFlags flags = leaf
                ? ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen
                : ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;

            if (_selectedNodeId == node.Id)
                flags |= ImGuiTreeNodeFlags.Selected;

            bool opened = ImGui.TreeNodeEx(node.Name, flags);
            if (ImGui.IsItemClicked())
                _selectedNodeId = node.Id;

            if (opened && !leaf)
            {
                foreach (var child in node.Children)
                    DrawNodeTree(child);
                ImGui.TreePop();
            }

            ImGui.PopID();
        }


        private static IEnumerable<Type> GetRegisteredScenes()
        {
            var field = typeof(SceneManager)
                .GetField("_registeredScenes", BindingFlags.NonPublic | BindingFlags.Static);
            var set = (HashSet<Type>)field!.GetValue(null)!;
            return set.OrderBy(t => t.Name);
        }

        private static Node3D FindNodeById(Node3D root, Guid id)
        {
            if (root.Id == id) return root;
            foreach (var c in root.Children)
                if (FindNodeById(c, id) is Node3D found)
                    return found;
            return null;
        }
    }

    static class ImGuiVectorExtensions
    {
        public static Vector3 ToSystemVector3(this OpenTK.Mathematics.Vector3 v) =>
            new Vector3(v.X, v.Y, v.Z);
        public static OpenTK.Mathematics.Vector3 ToOpenTKVector3(this Vector3 v) =>
            new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }
}
