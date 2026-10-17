
namespace Engine.Commands
{
    public class CameraRelativeRenderingCommand : CommandBase
    {
        public override string Name => "camRelRender";
        public override string Description => "Toggles camera relative render";

        public override void Execute(string[] args)
        {

            Camera cam = Camera.Main;

            if(cam != null)
            {
                RenderSpace.Toggle();
                Debug.Success("Camera Relative rendering: " + cam.CameraRelativeRender);
            }
        }
    }
}
