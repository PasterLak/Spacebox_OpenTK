using System.Numerics;

namespace Spacebox.Common.Commands
{
    public class VersionCommand : ICommand
    {
        public string Name => "version";
        public string Description => "Displays the game version.";

        public void Execute(string[] args)
        {
            string version = Application.Version; 
            GameConsole.AddMessage($"Game version: {version}", new Vector4(1f, 1f, 0f, 1f));
        }
    }
}
