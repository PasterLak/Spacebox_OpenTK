namespace Spacebox.Game.Disassembly;

public sealed record DisassemblySource(IReadOnlyList<Ingredient> Ingredients, byte ProductQuantity, int? DurationTicks);
