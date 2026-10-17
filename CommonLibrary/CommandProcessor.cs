
using System.Reflection;
using ServerCommon.Commands;

namespace ServerCommon
{
    public class CommandProcessor
    {
        private readonly ServerNetwork _server;
        private readonly ILogger _logger;
        private readonly PlayerManager _playerManager;
        private readonly Dictionary<string, IServerCommand> _commands = new Dictionary<string, IServerCommand>(StringComparer.OrdinalIgnoreCase);

        public Action OnClear { get; set; }
        public Action OnStop { get; set; }

        public ServerNetwork Server => _server;

        public CommandProcessor(ServerNetwork server, ILogger logger)
        {
            _server = server;
            _logger = logger;
            _playerManager = server.PlayerManager;

            RegisterCommands();
        }

        private void RegisterCommands()
        {
            var commandType = typeof(IServerCommand);
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => commandType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)

                .Where(t => t.Namespace != null && t.Namespace.StartsWith("ServerCommon.Commands"));

            foreach (var type in types)
            {
                try
                {
                    IServerCommand commandInstance;
                    var ctor = type.GetConstructor(new[] { typeof(CommandProcessor) });

                    if (ctor != null)
                    {
                        commandInstance = (IServerCommand)ctor.Invoke(new object[] { this });
                    }
                    else
                    {
                        commandInstance = (IServerCommand)Activator.CreateInstance(type);
                    }

                    if (commandInstance != null)
                    {
                        Register(commandInstance);
          
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Failed to register command {type.Name}: {ex.Message}", LogType.Error);
                }
            }

            int uniqueCommands = _commands.Values.Distinct().Count();
            //_logger.Log($"Loaded {uniqueCommands} unique commands.", LogType.Info);
        }

        public void Register(IServerCommand command)
        {
            if (_commands.ContainsKey(command.Name))
            {
                _logger.Log($"Command '{command.Name}' is already registered.", LogType.Warning);
                return;
            }

            _commands[command.Name] = command;
            foreach (var alias in command.Aliases)
            {
                if (!_commands.ContainsKey(alias))
                    _commands[alias] = command;
            }
        }

        public void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!input.StartsWith("/"))
            {
                //_server.BroadcastChat(-1, input);
                _logger.Log($"[Server]: {input}", LogType.Normal);
                return;
            }

            string cleanInput = input.Substring(1);
            string[] parts = cleanInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return;

            string commandName = parts[0];
            string[] args = parts.Skip(1).ToArray();
            string rawArgs = cleanInput.Length > commandName.Length ? cleanInput.Substring(commandName.Length + 1) : "";

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    var context = new CommandContext(_server, _logger, args, rawArgs);
                    command.Execute(context);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error executing command '{commandName}': {ex.Message}", LogType.Error);
                }
            }
            else
            {
                _logger.Log($"Unknown command: {commandName}. Type /help for a list of commands.", LogType.Warning);
            }
        }

        public IEnumerable<IServerCommand> GetAllCommands()
        {
            return _commands.Values.Distinct();
        }
    }
}