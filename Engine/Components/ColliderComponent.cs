using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine.Components
{
    public abstract class ColliderComponent : Component
    {
        public Vector3 Offset { get; set; } = Vector3.Zero;
        public bool UseManualSize { get; set; }
        public Vector3 ManualSize { get; set; } = Vector3.One;
        public float ManualRadius { get; set; } = 1f;
        public bool DebugCollision = true;
        public BoundingVolume Volume { get; protected set; }

        public override void OnAttached(Node3D owner)
        {
            base.OnAttached(owner);

            Recalculate();
            
        }

        public override void OnUpdate()
        {
            Recalculate();
            DrawDebug();
            if (VisualDebug.Enabled) DrawDebug();
        }

        protected abstract void Recalculate();
        protected abstract void DrawDebug();

        protected Vector3 GetOwnerWorldPositionWithOffset()
        {
            return Owner.PositionWorld + Offset;
        }
    }

    public class AABBCollider : ColliderComponent
    {
        public BoundingBox Box => (BoundingBox)Volume;

        protected override void Recalculate()
        {
            Vector3 size = UseManualSize ? ManualSize : Owner.Scale;
            Volume = new BoundingBox(GetOwnerWorldPositionWithOffset(), size);
        }

        protected override void DrawDebug()
        {
            if(DebugCollision)
            VisualDebug.DrawBoundingBox(Box, Color4.Yellow);
        }
    }

    public class OBBCollider : ColliderComponent
    {
        public BoundingBoxOBB OBB => (BoundingBoxOBB)Volume;

        protected override void Recalculate()
        {
            Vector3 size = UseManualSize ? ManualSize : Owner.Scale;
            Vector3 rad = new Vector3(
                MathHelper.DegreesToRadians(Owner.Rotation.X),
                MathHelper.DegreesToRadians(Owner.Rotation.Y),
                MathHelper.DegreesToRadians(Owner.Rotation.Z));
            Quaternion rot = Quaternion.FromEulerAngles(rad);
            Volume = new BoundingBoxOBB(GetOwnerWorldPositionWithOffset(), size, rot);
        }

        protected override void DrawDebug()
        {
            if (DebugCollision)
                VisualDebug.DrawOBB(OBB, Color4.Yellow);
        }
    }

    public class SphereCollider : ColliderComponent
    {
        public BoundingSphere Sphere => (BoundingSphere)Volume;

        protected override void Recalculate()
        {
            float radius = UseManualSize ? ManualRadius : Owner.Scale.X * 0.5f;
            Volume = new BoundingSphere(GetOwnerWorldPositionWithOffset(), radius);
        }

        protected override void DrawDebug()
        {
            if (DebugCollision)
                VisualDebug.DrawBoundingSphere(Sphere, Color4.Yellow);
        }
    }
}
