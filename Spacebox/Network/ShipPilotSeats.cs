using OpenTK.Mathematics;

namespace Spacebox.Network;

public readonly record struct PilotSeat(long EntityId, Vector3i Cell);

public sealed class ShipPilotSeats
{
    private readonly Dictionary<int, PilotSeat> _seats = new Dictionary<int, PilotSeat>();

    public int Count => _seats.Count;

    public bool TryTake(int peer, PilotSeat seat)
    {
        if (_seats.TryGetValue(peer, out var current) && current == seat) return true;
        if (IsTaken(seat)) return false;

        _seats[peer] = seat;
        return true;
    }

    public bool TryRelease(int peer, out PilotSeat seat)
    {
        return _seats.Remove(peer, out seat);
    }

    public bool IsPilotOf(int peer, PilotSeat seat)
    {
        return _seats.TryGetValue(peer, out var current) && current == seat;
    }

    public bool IsTaken(PilotSeat seat)
    {
        foreach (var current in _seats.Values)
            if (current == seat) return true;
        return false;
    }

    public void Clear()
    {
        _seats.Clear();
    }
}
