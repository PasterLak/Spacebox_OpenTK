using Engine;

namespace Spacebox.Game.Player.Armor;

public static class ArmorLibrary
{
    private static readonly Dictionary<short, ArmorPiece?> _pieces = new Dictionary<short, ArmorPiece?>();

    public static ArmorPiece? Get(EquipmentItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.ArmorModelPath) || string.IsNullOrEmpty(item.ArmorTexturePath)) return null;
        if (_pieces.TryGetValue(item.Id, out var cached)) return cached;

        var piece = Load(item);
        _pieces[item.Id] = piece;
        return piece;
    }

    public static void Clear()
    {
        _pieces.Clear();
    }

    private static ArmorPiece? Load(EquipmentItem item)
    {
        if (!File.Exists(item.ArmorModelPath) || !File.Exists(item.ArmorTexturePath)) return null;

        var mesh = Resources.Load<Mesh>(item.ArmorModelPath);
        var texture = Resources.Load<Texture2D>(item.ArmorTexturePath);
        if (!texture.YWasFlipped) texture.FlipY();
        texture.FilterMode = FilterMode.Nearest;

        return new ArmorPiece(mesh, new TextureMaterial(texture));
    }
}
