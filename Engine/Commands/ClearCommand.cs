

namespace Engine.Commands
{
    public class ClearCommand : CommandBase
    {
        public override string Name => "clear";
        public override string Description => "Clears all console messages";

        public override void Execute(string[] args)
        {
            Debug.ClearMessages();
           
        }
    }
}
