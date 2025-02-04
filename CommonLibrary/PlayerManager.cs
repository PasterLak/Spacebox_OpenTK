using SpaceNetwork;
using SpaceNetwork.Utilities;
using System.Numerics;

namespace ServerCommon;
public class PlayerManager
{
    private Dictionary<int, Player> players = new Dictionary<int, Player>();
    private int nextId = 1;
    private Random rand = new Random();

    public Player AddNewPlayer(string name)
    {
        var p = new Player
        {
            ID = nextId++,
            Name = name,
            Color = ColorHelper.HexToVector(ColorHelper.GetRandomColorFromListHex(rand)),
            Position = new Vector3(0f, 0f,0f),
            Rotation = new Vector4(0f, 0f, 0f,0f)
        };
        p.LastTimeWasActive = Environment.TickCount; 
        players[p.ID] = p;
        return p;
    }

    public void RemovePlayer(int id)
    {
        players.Remove(id);
    }

    public Dictionary<int, Player> GetAll() => players;

    public bool IsNameUsed(string name)
    {
        return players.Values.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void Reset()
    {
        players.Clear();
        nextId = 1;
    }

 
}
