using ImGuiNET;
using Engine.GUI;
using Spacebox.Game.Generation;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

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

            ImGui.SeparatorText("[WORLD]");
            ImGui.Text($"Sector: Pos: {World.CurrentSector.PositionWorld} Index: {World.CurrentSector.PositionIndex}");

            var cam = Camera.Main;

            if(cam != null) {

                var pos = cam.Position;

                if (World.CurrentSector != null && World.CurrentSector.BiomesMap != null)
                {
                    

                    var pos2 = World.CurrentSector.WorldToLocalPosition(cam.PositionWorld);

                    var biom = World.CurrentSector.BiomesMap.GetFromSectorLocalCoord(pos2);

                    ImGui.Text("Biome:"); ImGui.SameLine();
                    ImGui.TextColored(biom.DebugColor.ToSystemVector4(), $"{biom.Name}");

                }

                var p = World.CurrentSector.WorldToLocalPosition(pos);
                ImGui.Text("Position in:");
                ImGui.Text($"Sector: "+ Vector3Int(p));

                if(World.CurrentSector.IsPointInEntity(pos, out var entity))
                {
                    var p1 = entity.WorldPositionToLocal(pos);
                    ImGui.Text($"Entity: {Vector3Int(p1)} Index: {entity.PositionIndex}" );

                    if(entity.IsPositionInChunk(pos, out var chunk))
                    {
                        ImGui.Text($"Chunk: {entity.WorldPositionToBlockInChunk(pos)} Index: {chunk.PositionIndex}");
                    }
                }
               
            }
           

        }
    }
}
