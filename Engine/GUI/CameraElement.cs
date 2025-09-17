using ImGuiNET;
using OpenTK.Mathematics;

namespace Engine.GUI
{
    public class CameraElement : OverlayElement
    {

        public override void OnGUIText()
        {
            BeforeDraw?.Invoke();

            if (Camera.Main != null)
            {
                var cam = Camera.Main;
                Vector3i playerPosInt = new Vector3i((int)cam.Position.X, (int)cam.Position.Y, (int)cam.Position.Z);
                Vector3i playerRotInt = new Vector3i((int)cam.Rotation.X, (int)cam.Rotation.Y, (int)cam.Rotation.Z);
                //ImGui.Text("[Camera]");
                ImGui.SeparatorText("[CAMERA]");
               // ImGui.Separator();
                DrawVector("Pos: ", (int)cam.Position.X, (int)cam.Position.Y, (int)cam.Position.Z);
                ImGui.SameLine();
                DrawVector("| Rot: ", (int)cam.Rotation.X, (int)cam.Rotation.Y, (int)cam.Rotation.Z);
                ImGui.Text($"Camera-Relative Rendering:");
                ImGui.SameLine();
                if(cam.CameraRelativeRender)
                {
                    ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "True");
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "False");
                }
                    ImGui.Text($"[BVH] Visible:{BVHCuller.VisibleObjects} Culled:{BVHCuller.CulledObjects}");
               /// ImGui.Separator();
            }

            AfterDraw?.Invoke();
        }

        private void DrawVector(string text, int x , int y , int z)
        {
            ImGui.Text(text + "("); ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), x.ToString());
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), y.ToString());
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(0, 0, 1, 1), z.ToString());
             ImGui.SameLine(); ImGui.Text(")");
        }
    }
}
