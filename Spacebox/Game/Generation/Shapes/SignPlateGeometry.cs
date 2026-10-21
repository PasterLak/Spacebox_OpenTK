using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class SignPlateGeometry
{
    public const string SmallName = "sign_plate";
    public const string WideName = "sign_plate_wide";
    public const float Thickness = 1f / 32f;
    public static readonly Vector3 TextUp = -Vector3.UnitZ;

    public static string NameFor(int width) => width >= 2 ? WideName : SmallName;

    public static BlockGeometry Create(int width)
    {
        width = Math.Clamp(width, 1, 2);
        float t = Thickness;
        float end = width - 0.5f;
        var builder = new BlockGeometryBuilder();

        for (int i = 0; i < width; i++)
        {
            builder.AddQuad(new Vector3(i, t, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f), Vector2.Zero, Vector2.One);
            builder.AddQuad(new Vector3(i, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f), Vector2.Zero, Vector2.One);
        }

        float centerX = (end - 0.5f) * 0.5f;
        float halfLength = width * 0.5f;
        builder.AddQuad(new Vector3(centerX, t * 0.5f, 0.5f), new Vector3(halfLength, 0f, 0f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t));
        builder.AddQuad(new Vector3(centerX, t * 0.5f, -0.5f), new Vector3(-halfLength, 0f, 0f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t));
        builder.AddQuad(new Vector3(end, t * 0.5f, 0f), new Vector3(0f, 0f, -0.5f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t));
        builder.AddQuad(new Vector3(-0.5f, t * 0.5f, 0f), new Vector3(0f, 0f, 0.5f), new Vector3(0f, t * 0.5f, 0f), Vector2.Zero, new Vector2(1f, t));

        var built = builder.Build();
        return new BlockGeometry(built.Positions, built.Normals, built.TexCoords, built.Indices)
        {
            Anchor = new Vector3(0f, 0f, 0f),
            FacingUp = TextUp,
            RequiresSupport = true
        };
    }

    public static Vector3 ExtentDirection => Vector3.UnitX;
}
