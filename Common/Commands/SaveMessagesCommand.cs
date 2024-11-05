

namespace Spacebox.Common.Commands
{
    public class SaveMessagesCommand : ICommand
    {
        public string Name => "savelog";
        public string Description => "Save all console messages to file";

        public void Execute(string[] args)
        {
            Debug.AddMessage("Data: " + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            Debug.SaveMessagesToFile();
        }
    }
}
