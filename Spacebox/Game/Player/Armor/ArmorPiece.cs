using Engine;

namespace Spacebox.Game.Player.Armor;

public sealed class ArmorPiece
{
    public ArmorPiece(Mesh mesh, TextureMaterial material)
    {
        Mesh = mesh;
        Material = material;
    }

    public Mesh Mesh { get; }
    public TextureMaterial Material { get; }
}
