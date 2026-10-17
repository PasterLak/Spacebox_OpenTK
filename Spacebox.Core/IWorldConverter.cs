
namespace Spacebox.Core
{
    public interface IWorldConverter
    {
        string TargetGameVersion { get; }
        bool Convert(WorldInfo worldInfo);
    }
}
