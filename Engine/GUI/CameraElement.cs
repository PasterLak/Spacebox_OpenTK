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

                ImGui.Text($"Camera Pos: {playerPosInt} Rot: {playerRotInt}");
                ImGui.Text($"Camera-Relative Rendering: {cam.CameraRelativeRender}");
                ImGui.Text($"[BVH] Visible:{BVHCuller.VisibleObjects} Culled:{BVHCuller.CulledObjects}");

            }

            AfterDraw?.Invoke();
        }
    }
}
