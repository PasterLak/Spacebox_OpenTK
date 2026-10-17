using System.Numerics;

namespace Engine.Commands
{
    public class ExitCommand : CommandBase
    {
        public override string Name => "exit";
        public override string Description => "Closes the console";

        public override void Execute(string[] args)
        {
            Debug.ToggleVisibility();
          
        }
    }
}
