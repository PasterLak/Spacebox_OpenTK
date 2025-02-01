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
            Debug.AddMessage("Console closed.", new Vector4(1f, 1f, 1f, 1f));
        }
    }
}
