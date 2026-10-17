using System.Numerics;

namespace Engine.Commands
{
    public class VersionCommand : CommandBase
    {
        public override string Name => "version";
        public override string Description => "Displays the game version";

        public override void Execute(string[] args)
        {
            //string version = Application.Version; 
           // Debug.AddMessage($"Game version: {version}", new Vector4(1f, 1f, 0f, 1f));
        }
    }
}
