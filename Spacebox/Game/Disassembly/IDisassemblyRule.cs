namespace Spacebox.Game.Disassembly;

public interface IDisassemblyRule
{
    bool TryResolve(Item item, out DisassemblyRecipe? recipe);
}
