
namespace Spacebox.Scenes.Test
{
    public interface INode
    {
        string Name { get; set; }
        string Tag { get; set; }
        Guid Id { get; }
    }
}
