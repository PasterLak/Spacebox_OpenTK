using ImGuiNET;
using OpenTK.Mathematics;

namespace Spacebox.Engine.GUI
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
                ImGui.Text($"Camera Position: {playerPosInt}");
                ImGui.Text($"Camera-Relative Rendering: {cam.CameraRelativeRender}");
                
            }

            AfterDraw?.Invoke();
        }
    }
}
