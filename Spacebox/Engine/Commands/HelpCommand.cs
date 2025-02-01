using System.Numerics;

namespace Spacebox.Engine.Commands
{
    public class HelpCommand : ICommand
    {
        public string Name => "help";
        public string Description => "Displays all available commands.";

        public void Execute(string[] args)
        {
            Debug.AddMessage("Available commands:", new Vector4(0.5f, 0.5f, 1f, 1f));
            foreach (var command in CommandManager.GetCommands())
            {
                Debug.AddMessage($"- {command.Name}: {command.Description}", new Vector4(0.5f, 0.5f, 1f, 1f));
            }
        }
    }
}
