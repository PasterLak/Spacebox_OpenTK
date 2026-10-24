using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public static class AxesGizmo
{
    private static readonly Vector3[] Axes = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
    private static readonly string[] Labels = { "X", "Y", "Z" };
    private static readonly Color4[] Colors = { new Color4(1f, 0.25f, 0.25f, 1f), new Color4(0.3f, 1f, 0.3f, 1f), new Color4(0.35f, 0.55f, 1f, 1f) };
    private static readonly int[] Order = new int[3];

    public static Vector2 Project(Vector3 axis, Vector3 right, Vector3 up)
    {
        return new Vector2(Vector3.Dot(axis, right), -Vector3.Dot(axis, up));
    }

    public static float Depth(Vector3 axis, Vector3 front) => Vector3.Dot(axis, front);

    public static void SortBackToFront(Vector3 front, int[] order)
    {
        for (int i = 0; i < 3; i++) order[i] = i;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2 - i; j++)
            {
                if (Depth(Axes[order[j]], front) >= Depth(Axes[order[j + 1]], front)) continue;
                (order[j], order[j + 1]) = (order[j + 1], order[j]);
            }
        }
    }

    private static void SortBackToFront(Vector3 front) => SortBackToFront(front, Order);

    public static void Draw(ICanvasRenderer renderer, CanvasFont font, float fontSize, Camera camera, Vector2 center, float length)
    {
        if (camera == null) return;

        SortBackToFront(camera.Front);

        for (int i = 0; i < 3; i++)
        {
            int axis = Order[i];
            var end = center + Project(Axes[axis], camera.Right, camera.Up) * length;
            CanvasShapes.Line(renderer, center, end, Colors[axis], 2f);
            if (font != null) CanvasDraw.Text(renderer, font, end, Labels[axis], fontSize, Colors[axis]);
        }
    }
}
