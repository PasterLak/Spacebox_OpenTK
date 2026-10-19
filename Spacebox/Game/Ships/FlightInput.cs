namespace Spacebox.Game.Ships;

[Flags]
public enum FlightInput : ushort
{
    None = 0,
    Forward = 1 << 0,
    Backward = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Up = 1 << 4,
    Down = 1 << 5,
    RollLeft = 1 << 6,
    RollRight = 1 << 7,
    PitchUp = 1 << 8,
    PitchDown = 1 << 9,
    YawLeft = 1 << 10,
    YawRight = 1 << 11,
    Brake = 1 << 12,
    Steer = 1 << 13,

    Translation = Forward | Backward | Left | Right | Up | Down,
    SteerRotation = PitchUp | PitchDown | YawLeft | YawRight
}
