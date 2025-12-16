using System.Numerics;

namespace Engine.Commands
{
    public class HelpCommand : CommandBase
    {
        public override string Name => "help";
        public override string Description => "Displays all available commands";

        public override void Execute(string[] args)
        {
            Debug.Log("Available commands:", new Vector4(0.5f, 0.5f, 1f, 1f));
            foreach (var command in CommandManager.GetCommands())
            {
                Debug.Log($"- {command.Name}: {command.Description}", new Vector4(0.5f, 0.5f, 1f, 1f));
            }
        }
    }
}
