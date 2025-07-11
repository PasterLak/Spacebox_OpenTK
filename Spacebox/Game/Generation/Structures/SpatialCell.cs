using Engine;
using Engine.Physics;
using OpenTK.Mathematics;


namespace Spacebox.Game.Generation
{
    public class SpatialCell : Node3D
    {
        public new Vector3 PositionWorld { get; protected set; }
        public Vector3i PositionIndex { get; protected set; }

        public BoundingBox BoundingBox { get; protected set; }

        public override string ToString()
        {
            return $"[SpatialCell] Pos: {PositionWorld.ToString()} Index: {PositionIndex.ToString()}";
        }
    }
}
