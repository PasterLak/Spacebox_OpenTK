

namespace ServerCommon.Commands
{
    public class ClearCommand : IServerCommand
    {
        private readonly CommandProcessor _processor;
        public string Name => "clear";
        public string Description => "Clears the console output.";
        public string Usage => "/clear";
        public string[] Aliases => new[] { "cls" };

        public ClearCommand(CommandProcessor processor) => _processor = processor;

        public void Execute(CommandContext context)
        {
            _processor.OnClear?.Invoke();
        }
    }
}
