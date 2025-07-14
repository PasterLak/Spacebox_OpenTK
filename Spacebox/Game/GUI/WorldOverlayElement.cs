using ImGuiNET;
using Engine.GUI;
using Spacebox.Game.Generation;
using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public class WorldOverlayElement : OverlayElement
    {
        World world;
        public WorldOverlayElement(World world)
        {
            this.world = world;
        }

        public string Vector3Int(Vector3 p)
        {
            return $"x {(int)p.X} y {(int)p.Y} z {(int)p.Z}";
        }
        public override void OnGUIText()
        {
            if(world == null) { return; }
            if (World.CurrentSector == null) { return; }

            ImGui.Text($" ");
            ImGui.Text($"Sector. Pos: {World.CurrentSector.PositionWorld} Index: {World.CurrentSector.PositionIndex}");

            var cam = Camera.Main;

            if(cam != null) {

                var pos = cam.Position;

                var p = World.CurrentSector.WorldToLocalPosition(pos);
                ImGui.Text($"Pos in sector: "+ Vector3Int(p));

                if(World.CurrentSector.IsPointInEntity(pos, out var entity))
                {
                    var p1 = entity.WorldPositionToLocal(pos);
                    ImGui.Text($"Pos in entity: " + Vector3Int(p1));

                    if(entity.IsPositionInChunk(pos, out var chunk))
                    {
                        ImGui.Text($"Pos in chunk: {entity.WorldPositionToBlockInChunk(pos)} ");
                    }
                }
               
            }
            
        }
    }
}
