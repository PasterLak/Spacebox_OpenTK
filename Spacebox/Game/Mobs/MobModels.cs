using Engine;

namespace Spacebox.Game.Mobs;

public static class MobModels
{
    private static readonly Dictionary<string, Model> _models = new Dictionary<string, Model>();

    public static Model Get(MobDefinition definition)
    {
        if (_models.TryGetValue(definition.Id, out var model)) return model;

        var texture = GameAssets.LoadResource<Texture2D>(definition.TexturePath);
        if (!texture.YWasFlipped) texture.FlipY();
        texture.FilterMode = FilterMode.Nearest;

        model = new Model(GameAssets.LoadResource<Mesh>(definition.ModelPath), new TextureMaterial(texture));
        _models[definition.Id] = model;
        return model;
    }

    public static void Clear()
    {
        _models.Clear();
    }
}
