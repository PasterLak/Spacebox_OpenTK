using ImGuiNET;
using OpenTK.Mathematics;

namespace Engine.GUI;

public static class AxesGizmo
{
    private static readonly Vector3[] Axes = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
    private static readonly string[] Labels = { "X", "Y", "Z" };
    private static readonly uint[] Colors = { Pack(255, 64, 64), Pack(76, 255, 76), Pack(90, 140, 255) };
    private static readonly int[] Order = new int[3];

    private static uint Pack(uint r, uint g, uint b) => 0xFF000000u | (b << 16) | (g << 8) | r;

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

    public static void Draw(Camera camera, System.Numerics.Vector2 center, float length)
    {
        if (camera == null) return;

        SortBackToFront(camera.Front);

        var drawList = ImGui.GetForegroundDrawList();
        for (int i = 0; i < 3; i++)
        {
            int axis = Order[i];
            var projected = Project(Axes[axis], camera.Right, camera.Up) * length;
            var end = new System.Numerics.Vector2(center.X + projected.X, center.Y + projected.Y);
            drawList.AddLine(center, end, Colors[axis], 2f);
            drawList.AddText(end, Colors[axis], Labels[axis]);
        }
    }
}
