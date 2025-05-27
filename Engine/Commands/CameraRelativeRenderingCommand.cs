using System.Numerics;

namespace Engine.Commands
{
    public class CameraRelativeRenderingCommand : ICommand
    {
        public string Name => "camRelRender";
        public string Description => "";

        public void Execute(string[] args)
        {

            Camera cam = Camera.Main;

            if(cam != null)
            {
                cam.CameraRelativeRender = !cam.CameraRelativeRender;
                Debug.Success("Camera Relative rendering: " + cam.CameraRelativeRender);
            }
        }
    }
}
