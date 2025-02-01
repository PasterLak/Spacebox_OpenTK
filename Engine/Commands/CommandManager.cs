using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Commands
{
    public static class CommandManager
    {
        private static List<ICommand> _commands = new List<ICommand>();

        public static void RegisterCommand(ICommand command)
        {
            if (!_commands.Any(c => c.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _commands.Add(command);
            }
            else
            {
                Console.WriteLine($"Command '{command.Name}' is already registered.");
            }
        }

        public static ICommand GetCommand(string name)
        {
            return _commands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<ICommand> GetCommands()
        {
            return _commands;
        }

        public static IEnumerable<ICommand> FindCommandsStartingWith(string prefix)
        {
            return _commands.Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
