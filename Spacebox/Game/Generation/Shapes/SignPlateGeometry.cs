using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class SignPlateGeometry
{
    public const string Name = "sign_plate";
    public const float Thickness = 1f / 32f;
    public static readonly Vector3 TextUp = -Vector3.UnitZ;
    public static readonly Vector3 TextRight = Vector3.UnitX;

    public static BlockGeometry Create()
    {
        float t = Thickness;
        var built = new BlockGeometryBuilder()
            .AddQuad(new Vector3(0f, t, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f), Vector2.Zero, Vector2.One)
            .AddQuad(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f), Vector2.Zero, Vector2.One)
            .AddQuad(new Vector3(0f, t * 0.5f, 0.5f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t))
            .AddQuad(new Vector3(0f, t * 0.5f, -0.5f), new Vector3(-0.5f, 0f, 0f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t))
            .AddQuad(new Vector3(0.5f, t * 0.5f, 0f), new Vector3(0f, 0f, -0.5f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t))
            .AddQuad(new Vector3(-0.5f, t * 0.5f, 0f), new Vector3(0f, 0f, 0.5f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t))
            .Build();

        return new BlockGeometry(built.Positions, built.Normals, built.TexCoords, built.Indices)
        {
            FacingUp = TextUp,
            RequiresSupport = true
        };
    }
}
