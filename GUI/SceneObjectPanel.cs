
using ImGuiNET;
using Spacebox.Common;
using Spacebox.Extensions;
using System.Numerics;

namespace Spacebox.UI
{
    public class SceneObjectPanel
    {
        public static bool IsVisible { get; set; } = false;

        public static void Render(List<Node3D> _transforms)
        {
            if (!IsVisible)
                return;

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

            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.15f, 0.15f, 0.15f, 0.95f));

            ImGui.Begin(" ", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            ImGui.Separator();
            ImGui.Text("Scene Objects");
            ImGui.Separator();

            foreach (var transform in _transforms)
            {
                if (ImGui.TreeNode($"##TreeNode_{transform.Id}", transform.Name))
                {
                    string newName = transform.Name;

                    // Calculate available width for X, Y, Z fields
                    float availableWidth = ImGui.GetContentRegionAvail().X;
                    float labelWidth = 10.0f; // Width for the colored dot and label
                    float inputWidth = (availableWidth - (labelWidth + 10) * 4) / 4; // 10 pixels spacing between fields


                    ImGui.Text("Name     ");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth * 3);
                    if (ImGui.InputText($"##{transform.Id}", ref newName, 100))
                    {
                        transform.Name = newName;
                    }
                    ImGui.PopItemWidth();

                    ImGui.Separator();

                    ImGui.Text("Position");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    // Position X
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "X");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float posX = transform.Position.X;
                    if (ImGui.DragFloat($"##posx{transform.Id}", ref posX, 0.1f))
                    {
                        transform.Position = new Vector3(posX, transform.Position.Y, transform.Position.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Y
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float posY = transform.Position.Y;
                    if (ImGui.DragFloat($"##posy{transform.Id}", ref posY, 0.1f))
                    {
                        transform.Position = new Vector3(transform.Position.X, posY, transform.Position.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Z
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float posZ = transform.Position.Z;
                    if (ImGui.DragFloat($"##posz{transform.Id}", ref posZ, 0.1f))
                    {
                        transform.Position = new Vector3(transform.Position.X, transform.Position.Y, posZ).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();
                    // -----------------------------
                    // Rotation
                    Vector3 newRotation = transform.Rotation.ToSystemVector3();


                    ImGui.Text("Rotation");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    // Position X
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "X");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float rotX = transform.Rotation.X;
                    if (ImGui.DragFloat($"##rotx{transform.Id}", ref rotX, 0.1f))
                    {
                        transform.Rotation = new Vector3(rotX, transform.Rotation.Y, transform.Rotation.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Y
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float rotY = transform.Rotation.Y;
                    if (ImGui.DragFloat($"##roty{transform.Id}", ref rotY, 0.1f))
                    {
                        transform.Rotation = new Vector3(transform.Rotation.X, rotY, transform.Rotation.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Z
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float rotZ = transform.Rotation.Z;
                    if (ImGui.DragFloat($"##rotz{transform.Id}", ref rotZ, 0.1f))
                    {
                        transform.Rotation = new Vector3(transform.Rotation.X, transform.Rotation.Y, rotZ).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();


                    // --------------------


                    // Scale
                    Vector3 newScale = transform.Scale.ToSystemVector3();


                    ImGui.Text("Scale   ");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    // Position X
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "X");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float scaleX = transform.Scale.X;
                    if (ImGui.DragFloat($"##scalex{transform.Id}", ref scaleX, 0.1f))
                    {
                        transform.Scale = new Vector3(scaleX, transform.Scale.Y, transform.Scale.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Y
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float scaleY = transform.Scale.Y;
                    if (ImGui.DragFloat($"##scaley{transform.Id}", ref scaleY, 0.1f))
                    {
                        transform.Scale = new Vector3(transform.Scale.X, scaleY, transform.Scale.Z).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();

                    // Position Z
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(inputWidth);
                    float scaleZ = transform.Scale.Z;
                    if (ImGui.DragFloat($"##scalez{transform.Id}", ref scaleZ, 0.1f))
                    {
                        transform.Scale = new Vector3(transform.Scale.X, transform.Scale.Y, scaleZ).ToOpenTKVector3();
                    }
                    ImGui.PopItemWidth();
                    ImGui.Separator();

                    Collision col = transform as Collision;
                    
                    if(col != null)
                    {
                        var c = col.IsStatic;
                        var t = col.IsTrigger;
                        ImGui.Text("Collision   ");
                        ImGui.Separator();
                        ImGui.Text("IsStatic   ");
                      
                        ImGui.SameLine();
                        ImGui.PushItemWidth(inputWidth);
                        if (ImGui.Checkbox($"##check{transform.Id}",ref c))
                        {
                           
                        }
                        ImGui.Text("IsTrigger   ");

                        ImGui.SameLine();
                        ImGui.PushItemWidth(inputWidth);
                        if (ImGui.Checkbox($"##trigger{transform.Id}", ref t))
                        {

                        }
                    }


                    Model model = transform as Model;

                    if(model != null)
                    {
                        Vector4 color = model.Material.Color.ToSystemVector4();
                        ImGui.Text("Model   ");
                        ImGui.Separator();
                        ImGui.Text("Material  Color ");
                        ImGui.PushItemWidth(inputWidth*1.5f);
                        if (ImGui.ColorPicker4($"##color{transform.Id}", ref color))
                        {
                            model.Material.Color = color.ToOpenTKVector4();
                        }
                    }

                    ImGui.Separator();
                    ImGui.TreePop();
                }
            }

            ImGui.End();
            ImGui.PopStyleColor();

            Time.EndOnGUI();
        }
    }
}
