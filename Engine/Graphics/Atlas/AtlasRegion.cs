using OpenTK.Mathematics;

namespace Engine.Graphics.Atlas;

public sealed class AtlasRegion
{
    public string Name { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Vector2 UvMin { get; private set; }
    public Vector2 UvMax { get; private set; }

    public Vector2[] Uvs { get; } = new Vector2[4];

    public System.Numerics.Vector2 ImGuiUv0 => new System.Numerics.Vector2(UvMin.X, UvMax.Y);
    public System.Numerics.Vector2 ImGuiUv1 => new System.Numerics.Vector2(UvMax.X, UvMin.Y);

    internal AtlasRegion(string name)
    {
        Name = name;
    }

    internal void Set(int x, int y, int width, int height, int atlasWidth, int atlasHeight)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;

        UvMin = new Vector2(x / (float)atlasWidth, y / (float)atlasHeight);
        UvMax = new Vector2((x + width) / (float)atlasWidth, (y + height) / (float)atlasHeight);

        Uvs[0] = UvMin;
        Uvs[1] = new Vector2(UvMax.X, UvMin.Y);
        Uvs[2] = UvMax;
        Uvs[3] = new Vector2(UvMin.X, UvMax.Y);
    }

    public override string ToString()
    {
        return $"{Name}: {X},{Y} {Width}x{Height}";
    }
}
