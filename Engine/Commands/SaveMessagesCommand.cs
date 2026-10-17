

namespace Engine.Commands
{
    public class SaveMessagesCommand : CommandBase
    {
        public override string Name => "savelog";
        public override string Description => "Save all console messages to a file";

        public override void Execute(string[] args)
        {

            Debug.SaveMessagesToFile();
        }
    }
}
