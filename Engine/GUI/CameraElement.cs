using OpenTK.Mathematics;

namespace Engine.GUI
{
    public class CameraElement : OverlayElement
    {

        public override void Write(OverlayText text)
        {
            BeforeDraw?.Invoke();

            if (Camera.Main != null)
            {
                var cam = Camera.Main;
                //ImGui.Text("[Camera]");
                text.Section("[CAMERA]");
               // ImGui.Separator();
                DrawVector(text, "Pos: (", (int)cam.Position.X, (int)cam.Position.Y, (int)cam.Position.Z, true);
                DrawVector(text, "| Rot: (", (int)cam.Rotation.X, (int)cam.Rotation.Y, (int)cam.Rotation.Z, false);
                text.Line("Camera-Relative Rendering:");
                if(cam.CameraRelativeRender)
                {
                    text.Append("True", Overlay.Green);
                }
                else
                {
                    text.Append("False", Overlay.Red);
                }
                    text.Line($"[BVH] Visible:{BVHCuller.VisibleObjects} Culled:{BVHCuller.CulledObjects}");
               /// ImGui.Separator();
            }

            AfterDraw?.Invoke();
        }

        private void DrawVector(OverlayText text, string label, int x , int y , int z, bool newLine)
        {
            if (newLine) text.Line(label);
            else text.Append(label);
            text.Append(x.ToString(), Overlay.Red);
            text.Append(y.ToString(), Overlay.Green);
            text.Append(z.ToString(), Overlay.Blue);
            text.Append(")");
        }
    }
}
