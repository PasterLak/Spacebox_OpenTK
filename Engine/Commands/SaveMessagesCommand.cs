

namespace Engine.Commands
{
    public class SaveMessagesCommand : ICommand
    {
        public string Name => "savelog";
        public string Description => "Save all console messages to file";

        public void Execute(string[] args)
        {

            Debug.SaveMessagesToFile();
        }
    }
}
