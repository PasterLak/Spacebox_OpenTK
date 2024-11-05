using System.Numerics;

namespace Spacebox.Common.Commands
{
    public class ClearCommand : ICommand
    {
        public string Name => "clear";
        public string Description => "Clears all console messages.";

        public void Execute(string[] args)
        {
            Debug.ClearMessages();
            Debug.AddMessage("Console cleared.", new Vector4(1f, 1f, 0f, 1f));
        }
    }
}
