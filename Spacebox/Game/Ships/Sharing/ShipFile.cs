using OpenTK.Mathematics;
using SharpNBT;

namespace Spacebox.Game.Ships.Sharing;

public sealed class ShipFile
{
    public const int FormatVersion = 1;
    public const string Extension = ".ship";

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public List<string> GameSets { get; set; } = new List<string>();
    public string Created { get; set; } = "";
    public int Blocks { get; set; }
    public Vector3i Min { get; set; }
    public Vector3i Max { get; set; }
    public byte[] Picture { get; set; }
    public CompoundTag Entity { get; set; }

    public Vector3i Size => Max - Min;
}
