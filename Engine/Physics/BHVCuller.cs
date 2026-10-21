
using Engine.Components;
using Engine.Physics;
using Engine.Graphics;
using Engine.Graphics.Occlusion;
using OpenTK.Mathematics;

namespace Engine
{
    public static class BVHCuller
    {
        private readonly struct Bounds
        {
            public readonly Vector3 Min;
            public readonly Vector3 Max;

            public Bounds(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }

            public Bounds Union(Bounds other) => new Bounds(Vector3.ComponentMin(Min, other.Min), Vector3.ComponentMax(Max, other.Max));

            public bool Contains(Vector3 point) =>
                point.X >= Min.X && point.Y >= Min.Y && point.Z >= Min.Z &&
                point.X <= Max.X && point.Y <= Max.Y && point.Z <= Max.Z;
        }

        private static readonly Dictionary<Node3D, Bounds> cache = new();
        public static bool DebugGroups = true;
        public static Color4 DebugColor = new(1f, 0f, 1f, 1f);

        public static int VisibleObjects, CulledObjects;

        static readonly Prof.Token T_SceneBVH = Prof.RegisterTimer("BVH.Build");

        public static void Render(Node3D root, Camera cam)
        {
            if (root == null || cam == null) return;

            cache.Clear();
            using(Prof.Time(T_SceneBVH))
            Build(root, cam);

            VisibleObjects = CulledObjects = 0;
            Draw(root, cam);

           // foreach (var n in visibleNames)
            //    Debug.Log(n);

           
        }

        /* ---------- Build -------------------------------------------------- */

        private static void Build(Node3D node, Camera cam)
        {
            if (ReferenceEquals(node, cam)) return;

            bool hasBox = TryGetNodeBounds(node, out var box);
            if (!hasBox)
            {
                foreach (var ch in node.Children) Build(ch, cam);
                return;
            }
            if (DebugGroups && VisualDebug.Enabled) VisualDebug.DrawBoundingBox(BoundingBox.CreateFromMinMax(box.Min, box.Max), DebugColor);
            cache[node] = box;

            

            foreach (var ch in node.Children) Build(ch, cam);
        }

        /* ---------- Draw --------------------------------------------------- */

        private static void Draw(Node3D node, Camera cam)
        {
            bool hasCol = cache.TryGetValue(node, out var box);
            bool inside = true;

            if (hasCol)
            {
                inside = box.Contains(cam.Position) || cam.Frustum.IsBoxInFrustum(box.Min, box.Max);
                if (inside && node is IOcclusionCullable cullable) inside = PassesOcclusion(cullable, box, cam);
                //if (DebugGroups) VisualDebug.DrawBoundingBox(box, DebugColor);
            }

            if (!hasCol || inside)
            {
                node.Render();
                if (hasCol)
                {
                    VisibleObjects++;
                }
            }
            else                      
            {
                CulledObjects++;
            }

            foreach (var ch in node.Children)
            {
                if (ch.Enabled)
                {
                    Draw(ch, cam);
                }
            }
        }

        /* ---------- Helpers ------------------------------------------------ */

        private const float OcclusionNearDistance = 3f;

        private static bool TryGetNodeBounds(Node3D node, out Bounds box)
        {
            if (node is IRenderBounds renderBounds)
            {
                bool has = renderBounds.TryGetRenderBounds(out var min, out var max);
                box = new Bounds(min, max);
                return has;
            }

            bool hasBox = false;
            box = default;
            foreach (var c in node.Components)
            {
                if (!c.Enabled || c is not ColliderComponent col || !TryGetBounds(col.Volume, out var bounds)) continue;
                box = hasBox ? box.Union(bounds) : bounds;
                hasBox = true;
            }
            return hasBox;
        }

        private static bool PassesOcclusion(IOcclusionCullable cullable, Bounds box, Camera cam)
        {
            var culler = EngineWindow.Instance?.Occlusion;
            if (culler == null || cullable.Occlusion == null) return true;

            var near = new Vector3(OcclusionNearDistance);
            bool cameraNear = new Bounds(box.Min - near, box.Max + near).Contains(cam.Position);
            return culler.Prepare(cullable.Occlusion, cameraNear, Matrix4.Identity, RenderSpace.ToRender(box.Min), RenderSpace.ToRender(box.Max));
        }

        private static bool TryGetBounds(BoundingVolume v, out Bounds bounds)
        {
            switch (v)
            {
                case BoundingBox bb:
                    bounds = new Bounds(bb.Min, bb.Max);
                    return true;
                case BoundingSphere s:
                    bounds = new Bounds(s.Center - new Vector3(s.Radius), s.Center + new Vector3(s.Radius));
                    return true;
                case BoundingBoxOBB o:
                    var extents = RotatedExtents(o.Extents, o.Rotation);
                    bounds = new Bounds(o.Center - extents, o.Center + extents);
                    return true;
                default:
                    bounds = default;
                    return false;
            }
        }

        private static Vector3 RotatedExtents(Vector3 extents, Quaternion rotation)
        {
            var x = Vector3.Transform(Vector3.UnitX, rotation);
            var y = Vector3.Transform(Vector3.UnitY, rotation);
            var z = Vector3.Transform(Vector3.UnitZ, rotation);
            return new Vector3(
                MathF.Abs(x.X) * extents.X + MathF.Abs(y.X) * extents.Y + MathF.Abs(z.X) * extents.Z,
                MathF.Abs(x.Y) * extents.X + MathF.Abs(y.Y) * extents.Y + MathF.Abs(z.Y) * extents.Z,
                MathF.Abs(x.Z) * extents.X + MathF.Abs(y.Z) * extents.Y + MathF.Abs(z.Z) * extents.Z);
        }
    }
}
