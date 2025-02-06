
using Engine.Commands;

using Client;

namespace Spacebox.Game.Commands
{
    internal class ChatCommand : ICommand
    {
        public string Name => "say";

        public string Description => "say to all";


        public ChatCommand()
        {
           
        }
        public void Execute(string[] args)
        {
            return;
            if (args.Length > 0)
            {

                if(ClientNetwork.Instance != null)
                {

                    string text = "";

                    for(int i = 0; i < args.Length; i++)
                    {
                        text += args[i] + " ";
                    }

                    ClientNetwork.Instance.SendMessage(text);
                }

                return;
            }

        }

      
    }
}
