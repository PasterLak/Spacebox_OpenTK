namespace Spacebox.Game.Player;

[Flags]
public enum PlayerStat : byte
{
    None = 0,
    Health = 1,
    Power = 2,
    Air = 4,
    All = Health | Power | Air
}
