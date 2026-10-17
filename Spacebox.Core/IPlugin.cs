namespace Spacebox.Core
{
   
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }

        void OnInitialize(IGameApi api);
    }
}