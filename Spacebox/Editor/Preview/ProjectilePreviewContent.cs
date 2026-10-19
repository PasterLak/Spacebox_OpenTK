using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Editor.Preview;

public sealed class ProjectilePreviewContent : IPreviewContent
{
    private const float TrackLength = 12f;
    private const float PreviewSpeedScale = 0.25f;

    private readonly LineRenderer _bullet = new LineRenderer();
    private readonly LineRenderer _track = new LineRenderer();
    private readonly LineRenderer _ruler = new LineRenderer();
    private float _position;

    public float Speed { get; set; }
    public float Length { get; set; }
    public float Thickness { get; set; }
    public Color4 Color { get; set; }

    public Vector3 Center => Vector3.Zero;
    public float Radius => TrackLength * 0.5f;
    public bool IsReady => true;
    public Vector2 ViewAngles => new Vector2(0f, 12f);

    public ProjectilePreviewContent()
    {
        _track.Color = new Color4(0.35f, 0.35f, 0.4f, 1f);
        _track.Thickness = 0.05f;
        _track.SetPoints(new List<Vector3> { new Vector3(-TrackLength * 0.5f, -0.6f, 0f), new Vector3(TrackLength * 0.5f, -0.6f, 0f) });

        _ruler.Color = new Color4(0.9f, 0.9f, 0.9f, 1f);
        _ruler.Thickness = 0.06f;
        _ruler.SetPoints(new List<Vector3> { new Vector3(-0.5f, -0.6f, 0.3f), new Vector3(0.5f, -0.6f, 0.3f) });

        _bullet.SetPoints(new List<Vector3> { Vector3.Zero, Vector3.UnitX });
    }

    public void Update(float deltaTime)
    {
        float length = MathF.Max(0.01f, Length);
        _position += MathF.Max(0.5f, Speed * PreviewSpeedScale) * deltaTime;
        if (_position > TrackLength + length) _position = 0f;

        float head = -TrackLength * 0.5f + _position;
        _bullet.Points[0] = new Vector3(head - length, 0f, 0f);
        _bullet.Points[1] = new Vector3(head, 0f, 0f);
        _bullet.Thickness = MathF.Max(0.03f, Thickness);
        _bullet.Color = Color;
        _bullet.SetNeedsRebuild();
    }

    public void Render()
    {
        _track.Render();
        _ruler.Render();
        _bullet.Render();
    }

    public void Dispose()
    {
        _bullet.Dispose();
        _track.Dispose();
        _ruler.Dispose();
    }
}
