using System.Numerics;

namespace Spacebox.Common.Commands
{
    public class ExitCommand : ICommand
    {
        public string Name => "exit";
        public string Description => "Closes the console.";

        public void Execute(string[] args)
        {
            GameConsole.ToggleVisibility();
            GameConsole.AddMessage("Console closed.", new Vector4(1f, 1f, 1f, 1f));
        }
    }
}
