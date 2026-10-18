namespace Spacebox.Game.Disassembly;

public interface IDisassemblySourceProvider
{
    void Collect(Item item, List<DisassemblySource> sources);
}
