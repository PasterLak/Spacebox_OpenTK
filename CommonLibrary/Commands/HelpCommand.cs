using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon.Commands
{
    public class HelpCommand : IServerCommand
    {
        private readonly CommandProcessor _processor;
        public string Name => "help";
        public string Description => "Shows a list of available commands.";
        public string Usage => "/help [command]";
        public string[] Aliases => new[] { "?" };

        public HelpCommand(CommandProcessor processor) => _processor = processor;

        public void Execute(CommandContext context)
        {
            if (context.Args.Length > 0)
            {

                context.Reply($"Usage: {Usage}");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("Available Commands:");
                foreach (var cmd in _processor.GetAllCommands().OrderBy(c => c.Name))
                {
                    sb.AppendLine($"  /{cmd.Name} - {cmd.Description}");
                }
                context.Reply(sb.ToString());
            }
        }
    }

}
