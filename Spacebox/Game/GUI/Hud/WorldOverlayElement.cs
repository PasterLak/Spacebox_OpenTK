using Engine.GUI;
using Spacebox.Game.Generation;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.GameMath;

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
        public override void Write(OverlayText text)
        {
            if (world == null) { return; }
            if (World.CurrentSector == null) { return; }

            text.Section("[WORLD]");
            text.Line($"Time: {GameTime.ToString()} Tick: {GameTime.DayTick} ");
            text.Line($"Sector: Pos: {World.CurrentSector.PositionWorld} Index: {World.CurrentSector.PositionIndex}");
            var occlusion = EngineWindow.Instance.Occlusion;
            text.Line(occlusion.Enabled
                ? $"Occlusion: drawn {occlusion.DrawnLastFrame}, occluded {occlusion.OccludedLastFrame}"
                : $"Occlusion: off, drawn {occlusion.DrawnLastFrame}");

            var cam = Camera.Main;

            if (cam != null)
            {

                var pos = cam.Position;

                if (World.CurrentSector != null && World.CurrentSector.BiomesMap != null)
                {


                    var pos2 = World.CurrentSector.WorldToLocalPosition(cam.PositionWorld);

                    var biom = World.CurrentSector.BiomesMap.GetFromSectorLocalCoord(pos2);

                    text.Line("Biome:");
                    if (biom != null) text.Append($"{biom.Name}", new Color4(biom.DebugColor.R / 255f, biom.DebugColor.G / 255f, biom.DebugColor.B / 255f, 1f));
                    else text.Append("empty space", Engine.CanvasUI.DarkTheme.TextDim);

                }

                var p = World.CurrentSector.WorldToLocalPosition(pos);
                text.Line("Position in:");
                text.Line($"Sector: " + Vector3Int(p));

                if (World.CurrentSector.IsPointInEntity(pos, out var entity))
                {
                    var p1 = entity.WorldPositionToLocal(entity.ToEntitySpace(pos));
                    text.Line($"Entity: {Vector3Int(p1)} Index: {entity.PositionIndex}");

                    if (entity.IsPositionInChunk(entity.ToEntitySpace(pos), out var chunk))
                    {

                        text.Line($"Chunk: {SpaceMath.Entity.WorldPositionToBlockInChunk(entity, entity.ToEntitySpace(pos))} Index: {chunk.PositionIndex}");
                    }
                }

            }


        }
    }
}
