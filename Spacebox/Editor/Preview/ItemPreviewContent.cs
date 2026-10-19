using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Editor.Preview;

public sealed class ItemPreviewContent : IPreviewContent
{
    private readonly Model _model;

    public Vector3 Center { get; }
    public float Radius { get; }
    public bool IsReady => _model != null;
    public Vector2 ViewAngles => new Vector2(15f, 10f);

    public ItemPreviewContent(Model model)
    {
        _model = model;
        if (model == null) return;

        model.Position = Vector3.Zero;
        model.Rotation = Vector3.Zero;
        Center = new Vector3(0.5f, 0.5f, 0f) * model.Scale;
        Radius = 0.75f * MathF.Max(model.Scale.X, model.Scale.Y);
    }

    public void Update(float deltaTime)
    {
    }

    public void Render()
    {
        _model?.Render();
    }

    public void Dispose()
    {
    }
}
