namespace Engine.Commands
{
    public static class CommandManager
    {
        private static List<CommandBase> _globalCommands = new List<CommandBase>();
        private static List<CommandBase> _sceneCommands = new List<CommandBase>();

        public static void RegisterGlobalCommand(CommandBase command)
        {
            if (IsCommandRegistered(command.Name))
            {
                Debug.Error($"[CommandManager] Global Command '{command.Name}' is already registered.");
                return;
            }
            _globalCommands.Add(command);
        }

        public static void RegisterSceneCommand(CommandBase command)
        {
            if (IsCommandRegistered(command.Name))
            {
                Debug.Error($"[CommandManager] Scene Command '{command.Name}' is already registered.");
                return;
            }
            _sceneCommands.Add(command);
        }

        public static void RegisterCommand(CommandBase command)
        {
            RegisterSceneCommand(command);
        }

        public static void ClearSceneCommands()
        {
            _sceneCommands.Clear();
        }

        private static bool IsCommandRegistered(string name)
        {
            return _globalCommands.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                   _sceneCommands.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static CommandBase GetCommand(string name)
        {
            var cmd = _sceneCommands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (cmd != null)
                return cmd;

            return _globalCommands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<CommandBase> GetCommands()
        {
            return _globalCommands.Concat(_sceneCommands);
        }

        public static IEnumerable<CommandBase> FindCommandsStartingWith(string prefix)
        {
            return _globalCommands.Concat(_sceneCommands)
                .Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}