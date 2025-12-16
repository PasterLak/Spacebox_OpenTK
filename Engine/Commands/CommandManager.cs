
namespace Engine.Commands
{
    public static class CommandManager
    {
        private static List<CommandBase> _commands = new List<CommandBase>();

        public static void RegisterCommand(CommandBase command)
        {
            if (!_commands.Any(c => c.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _commands.Add(command);
            }
            else
            {
                Debug.Error($"[CommandManager] Command '{command.Name}' is already registered.");
            }
        }

        public static CommandBase GetCommand(string name)
        {
            return _commands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<CommandBase> GetCommands()
        {
            return _commands;
        }

        public static IEnumerable<CommandBase> FindCommandsStartingWith(string prefix)
        {
            return _commands.Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
