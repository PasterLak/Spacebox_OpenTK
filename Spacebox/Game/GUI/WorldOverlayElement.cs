using ImGuiNET;
using Engine.GUI;
using Spacebox.Game.Generation;
using Engine;
namespace Spacebox.Game.GUI
{
    public class WorldOverlayElement : OverlayElement
    {
        World world;
        public WorldOverlayElement(World world)
        {
            this.world = world;
        }
        public override void OnGUIText()
        {
            if(world == null) { return; }
            if (World.CurrentSector == null) { return; }

            ImGui.Text($" ");
            ImGui.Text($"Sector. Pos: {World.CurrentSector.PositionWorld} Index: {World.CurrentSector.PositionIndex}");
        }
    }
}
