

using ImGuiNET;
using OpenTK.Mathematics;
using System.Numerics;


namespace Engine.UI
{
    public class Rect : Node2D
    {
        public Color4 Color4 { get; set; } = Color4.Pink;
        public Rect(OpenTK.Mathematics.Vector2 pos, OpenTK.Mathematics.Vector2 size)
        {
            Position = pos.ToSystemVector2();
            Size = size.ToSystemVector2();
            
        }

        public override void Draw()
        {
            base.Draw();

            ImGui.SetNextWindowPos(basePosition, ImGuiCond.Always);
            ImGui.SetNextWindowSize(_size, ImGuiCond.Always);

            ImGui.Begin("window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing |
                                         ImGuiWindowFlags.NoInputs);


            ImGui.GetWindowDrawList().AddRectFilled(
                basePosition,
                basePosition + _size,
                ImGui.ColorConvertFloat4ToU32(Color4.ToSystemVector4())
            );

           


            ImGui.End();
        }
    }
}
