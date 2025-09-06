using System.Numerics;

namespace Engine.Commands
{
    public class ExitCommand : ICommand
    {
        public string Name => "exit";
        public string Description => "Closes the console.";

        public void Execute(string[] args)
        {
            Debug.ToggleVisibility();
            Debug.Log("Console closed.");
        }
    }
}
