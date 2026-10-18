namespace Spacebox.Game.Disassembly;

public enum DisassemblerStatus : byte
{
    Idle,
    Working,
    OutputFull,
    NotEnoughItems,
    CannotDisassemble
}
