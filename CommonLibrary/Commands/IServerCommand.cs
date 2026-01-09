
namespace ServerCommon.Commands
{
    public interface IServerCommand
    {
        string Name { get; }
        string Description { get; }
        string Usage { get; }
        string[] Aliases { get; }
        void Execute(CommandContext context);
    }

}
