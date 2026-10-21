using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class PictureFrameGeometry
{
    public const float Pixel = 1f / 32f;
    public const float Thickness = Pixel;
    public const float RimHeight = Pixel;
    public const float Margin = Pixel;
    public const float RimWidth = Pixel;
    public const float PictureInset = Margin + RimWidth;
    public static readonly Vector3 PictureUp = -Vector3.UnitZ;

    private static readonly Lazy<BlockShape> _shape = new Lazy<BlockShape>(() => new BlockShape(Create(PlateEdges.All), Create));

    public static BlockShape Shape => _shape.Value;

    public static BlockGeometry Create(int edges)
    {
        var builder = new BlockGeometryBuilder();
        AddPlate(builder, edges);
        AddRims(builder, edges);
        var built = builder.Build();

        return new BlockGeometry(built.Positions, built.Normals, built.TexCoords, built.Indices)
        {
            FacingUp = PictureUp,
            RequiresSupport = true
        };
    }

    private static void AddPlate(BlockGeometryBuilder builder, int edges)
    {
        AddTop(builder, -0.5f, 0.5f, -0.5f, 0.5f, Thickness);
        builder.AddQuad(Vector3.Zero, new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f), Vector2.Zero, Vector2.One);

        if (PlateEdges.Has(edges, PlateEdges.Left)) AddSide(builder, new Vector3(-0.5f, 0f, 0f), -Vector3.UnitX, 1f, 0f, Thickness);
        if (PlateEdges.Has(edges, PlateEdges.Right)) AddSide(builder, new Vector3(0.5f, 0f, 0f), Vector3.UnitX, 1f, 0f, Thickness);
        if (PlateEdges.Has(edges, PlateEdges.Top)) AddSide(builder, new Vector3(0f, 0f, -0.5f), -Vector3.UnitZ, 1f, 0f, Thickness);
        if (PlateEdges.Has(edges, PlateEdges.Bottom)) AddSide(builder, new Vector3(0f, 0f, 0.5f), Vector3.UnitZ, 1f, 0f, Thickness);
    }

    private static void AddRims(BlockGeometryBuilder builder, int edges)
    {
        bool left = PlateEdges.Has(edges, PlateEdges.Left);
        bool right = PlateEdges.Has(edges, PlateEdges.Right);
        bool top = PlateEdges.Has(edges, PlateEdges.Top);
        bool bottom = PlateEdges.Has(edges, PlateEdges.Bottom);
        float outer = 0.5f - Margin;
        float inner = 0.5f - PictureInset;

        float z0 = top ? -outer : -0.5f;
        float z1 = bottom ? outer : 0.5f;
        if (left) AddVerticalRim(builder, -outer, -inner, z0, z1, top, bottom);
        if (right) AddVerticalRim(builder, inner, outer, z0, z1, top, bottom);

        float x0 = left ? -inner : -0.5f;
        float x1 = right ? inner : 0.5f;
        if (top) AddHorizontalRim(builder, x0, x1, -outer, -inner);
        if (bottom) AddHorizontalRim(builder, x0, x1, inner, outer);
    }

    private static void AddVerticalRim(BlockGeometryBuilder builder, float x0, float x1, float z0, float z1, bool capTop, bool capBottom)
    {
        float centerX = (x0 + x1) * 0.5f;
        float centerZ = (z0 + z1) * 0.5f;
        AddTop(builder, x0, x1, z0, z1, Thickness + RimHeight);
        AddSide(builder, new Vector3(x0, 0f, centerZ), -Vector3.UnitX, z1 - z0, Thickness, RimHeight);
        AddSide(builder, new Vector3(x1, 0f, centerZ), Vector3.UnitX, z1 - z0, Thickness, RimHeight);
        if (capTop) AddSide(builder, new Vector3(centerX, 0f, z0), -Vector3.UnitZ, x1 - x0, Thickness, RimHeight);
        if (capBottom) AddSide(builder, new Vector3(centerX, 0f, z1), Vector3.UnitZ, x1 - x0, Thickness, RimHeight);
    }

    private static void AddHorizontalRim(BlockGeometryBuilder builder, float x0, float x1, float z0, float z1)
    {
        float centerX = (x0 + x1) * 0.5f;
        AddTop(builder, x0, x1, z0, z1, Thickness + RimHeight);
        AddSide(builder, new Vector3(centerX, 0f, z0), -Vector3.UnitZ, x1 - x0, Thickness, RimHeight);
        AddSide(builder, new Vector3(centerX, 0f, z1), Vector3.UnitZ, x1 - x0, Thickness, RimHeight);
    }

    private static void AddTop(BlockGeometryBuilder builder, float x0, float x1, float z0, float z1, float y)
    {
        var center = new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f);
        builder.AddQuad(center, new Vector3((x1 - x0) * 0.5f, 0f, 0f), new Vector3(0f, 0f, -(z1 - z0) * 0.5f),
            new Vector2(x0 + 0.5f, 0.5f - z1), new Vector2(x1 + 0.5f, 0.5f - z0));
    }

    private static void AddSide(BlockGeometryBuilder builder, Vector3 edgeCenter, Vector3 normal, float length, float bottom, float height)
    {
        var center = edgeCenter + new Vector3(0f, bottom + height * 0.5f, 0f);
        var right = Vector3.Cross(Vector3.UnitY, normal) * (length * 0.5f);
        var up = new Vector3(0f, height * 0.5f, 0f);
        builder.AddQuad(center, right, up, Vector2.Zero, new Vector2(length, height));
    }
}
