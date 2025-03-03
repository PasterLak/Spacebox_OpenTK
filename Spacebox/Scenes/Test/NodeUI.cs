using ImGuiNET;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Engine;

namespace Spacebox.FPS.GUI
{
    public class NodeUI
    {
        private static bool _isVisible = false;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (_isVisible)
                {
                    Input.ShowCursor();
                }
                else
                {
                    Input.HideCursor();
                }
            }
        }

        /// <summary>
        /// Renders a window with scene objects.
        /// Recursively displays properties for each SceneNode.
        /// </summary>
        /// <param name="sceneNodes">List of root SceneNodes.</param>
        public static void Render(List<SceneNode> sceneNodes)
        {
            if (!IsVisible)
                return;

            

            // Start timing for ImGui performance if needed
            Time.StartOnGUI();

            var io = ImGui.GetIO();
            Vector2 displaySize = new Vector2(io.DisplaySize.X, io.DisplaySize.Y);

            float windowWidthRatio = 0.3f;
            float windowHeightRatio = 1.0f;

            Vector2 windowSize = new Vector2(
                displaySize.X * windowWidthRatio,
                displaySize.Y * windowHeightRatio
            );

            Vector2 windowPos = new Vector2(
                displaySize.X - windowSize.X - 10,
                10
            );

            ImGui.SetNextWindowSize(windowSize.ToSystemVector2(), ImGuiCond.Always);
            ImGui.SetNextWindowPos(windowPos.ToSystemVector2(), ImGuiCond.Always);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.15f, 0.15f, 0.15f, 0.95f).ToSystemVector4());

            ImGui.Begin("Scene Objects", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
            ImGui.Separator();

            // Render each root node recursively.
            foreach (var node in sceneNodes)
            {
                RenderSceneNode(node);
            }

            ImGui.End();
            ImGui.PopStyleColor();

            Time.EndOnGUI();
        }

        /// <summary>
        /// Recursively renders a SceneNode and its children.
        /// </summary>
        /// <param name="node">The SceneNode to render.</param>
        private static void RenderSceneNode(SceneNode node)
        {
            // Use the node's unique Id to generate a unique tree node label.
            if (ImGui.TreeNode($"##TreeNode_{node.Id}", node.Name))
            {
                // Editable Name.
                string newName = node.Name;
                ImGui.Text("Name:");
                ImGui.SameLine();
                ImGui.PushItemWidth(200);
                if (ImGui.InputText($"##name_{node.Id}", ref newName, 100))
                {
                    node.Name = newName;
                }
                ImGui.PopItemWidth();
                ImGui.Separator();

                // Position.
                ImGui.Text("Position:");
                ImGui.SameLine();
                float posX = node.Position.X, posY = node.Position.Y, posZ = node.Position.Z;
                ImGui.PushItemWidth(60);
                if (ImGui.DragFloat($"X##pos_{node.Id}", ref posX, 0.1f))
                    node.Position = new Vector3(posX, node.Position.Y, node.Position.Z);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Y##pos_{node.Id}", ref posY, 0.1f))
                    node.Position = new Vector3(node.Position.X, posY, node.Position.Z);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Z##pos_{node.Id}", ref posZ, 0.1f))
                    node.Position = new Vector3(node.Position.X, node.Position.Y, posZ);
                ImGui.PopItemWidth();
                ImGui.Separator();

                // Rotation.
                // Здесь упрощённо выводим компоненты Quaternion (X, Y, Z).
                // Для корректного редактирования можно конвертировать в Euler-углы.
                ImGui.Text("Rotation:");
                ImGui.SameLine();
                float rotX = node.Rotation.X, rotY = node.Rotation.Y, rotZ = node.Rotation.Z;
                ImGui.PushItemWidth(60);
                if (ImGui.DragFloat($"X##rot_{node.Id}", ref rotX, 0.1f))
                    node.Rotation = new Quaternion(rotX, node.Rotation.Y, node.Rotation.Z, node.Rotation.W);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Y##rot_{node.Id}", ref rotY, 0.1f))
                    node.Rotation = new Quaternion(node.Rotation.X, rotY, node.Rotation.Z, node.Rotation.W);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Z##rot_{node.Id}", ref rotZ, 0.1f))
                    node.Rotation = new Quaternion(node.Rotation.X, node.Rotation.Y, rotZ, node.Rotation.W);
                ImGui.PopItemWidth();
                ImGui.Separator();

                // Scale.
                ImGui.Text("Scale:");
                ImGui.SameLine();
                float scaleX = node.Scale.X, scaleY = node.Scale.Y, scaleZ = node.Scale.Z;
                ImGui.PushItemWidth(60);
                if (ImGui.DragFloat($"X##scale_{node.Id}", ref scaleX, 0.1f))
                    node.Scale = new Vector3(scaleX, node.Scale.Y, node.Scale.Z);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Y##scale_{node.Id}", ref scaleY, 0.1f))
                    node.Scale = new Vector3(node.Scale.X, scaleY, node.Scale.Z);
                ImGui.SameLine();
                if (ImGui.DragFloat($"Z##scale_{node.Id}", ref scaleZ, 0.1f))
                    node.Scale = new Vector3(node.Scale.X, node.Scale.Y, scaleZ);
                ImGui.PopItemWidth();
                ImGui.Separator();

                // Recursively render children.
                foreach (var child in node.Children)
                {
                    RenderSceneNode(child);
                }

                ImGui.TreePop();
            }
        }
    }
}
