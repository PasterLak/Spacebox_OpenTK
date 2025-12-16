
using Engine.Commands;

using Client;

namespace Spacebox.Game.Commands
{
    internal class ChatCommand : CommandBase
    {
        public override string Name => "say";

        public override string Description => "Say to all";


        public ChatCommand()
        {
           
        }
        public override void Execute(string[] args)
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
