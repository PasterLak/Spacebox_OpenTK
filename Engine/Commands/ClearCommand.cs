using System.Numerics;

namespace Engine.Commands
{
    public class ClearCommand : ICommand
    {
        public string Name => "clear";
        public string Description => "Clears all console messages.";

        public void Execute(string[] args)
        {
            Debug.ClearMessages();
           
        }
    }
}
