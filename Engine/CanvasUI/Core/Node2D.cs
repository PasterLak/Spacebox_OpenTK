using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class Node2D : CanvasItem
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;

    public Transform2D LocalTransform => Transform2D.Create(Position, Rotation, Scale);

    public override Transform2D GlobalTransform => ParentTransform() * LocalTransform;

    public Vector2 GlobalPosition => GlobalTransform.Origin;

    private Transform2D ParentTransform()
    {
        return Parent?.GlobalTransform ?? Transform2D.Identity;
    }
}
