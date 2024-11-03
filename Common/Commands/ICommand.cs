namespace Spacebox.Common.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(string[] args);
    }
}
