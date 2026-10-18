using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public struct ConsoleCommand
{
    public ushort Buttons;
    public FlightInput Flight;
    public Vector3 AimLocal;
    public int PilotPeer;

    public bool IsIdle => Buttons == 0 && Flight == FlightInput.None;
}
