
using Engine.Components;
using Engine.Physics;
using OpenTK.Mathematics;

namespace Engine
{
    public static class BVHCuller
    {
        private static readonly Dictionary<Node3D, BoundingBox> cache = new();
        private static readonly List<string> visibleNames = new();
        public static bool DebugGroups = true;
        public static Color4 DebugColor = new(1f, 0f, 1f, 1f);
        public static int VisibleObjects, CulledObjects;

        static readonly Prof.Token T_SceneBVH = Prof.RegisterTimer("BVH.Build");

        public static void Render(Node3D root, Camera cam)
        {
            if (root == null || cam == null) return;

            cache.Clear();
            visibleNames.Clear();
            using(Prof.Time(T_SceneBVH))
            Build(root);

            VisibleObjects = CulledObjects = 0;
            Draw(root, cam);

           // foreach (var n in visibleNames)
            //    Debug.Log(n);

           
        }

        /* ---------- Build -------------------------------------------------- */

        private static void Build(Node3D node)
        {
            if (node is Camera) return;
            if (!HasCollider(node))
            {
                foreach (var ch in node.Children) Build(ch);
                return;
            }

            BoundingBox? box = null;
            foreach (var c in node.Components)
                if (c.Enabled && c is ColliderComponent col)
                    box = Union(box, ToAABB(col.Volume));
            if (DebugGroups) VisualDebug.DrawBoundingBox(box, DebugColor);
            if (box != null)
                cache[node] = box;

            

            foreach (var ch in node.Children) Build(ch);
        }

        /* ---------- Draw --------------------------------------------------- */

        private static void Draw(Node3D node, Camera cam)
        {
            bool hasCol = cache.TryGetValue(node, out var box);
            bool inside = true;

            if (hasCol)
            {
                inside = box.Contains(cam.Position) || cam.Frustum.IsInFrustum(box);
                //if (DebugGroups) VisualDebug.DrawBoundingBox(box, DebugColor);
            }

            if (!hasCol || inside)
            {
                node.Render();
                if (hasCol)
                {
                    VisibleObjects++;
                    visibleNames.Add(node.Name);
                }
            }
            else                      
            {
                CulledObjects++;
            }

            foreach (var ch in node.Children) Draw(ch, cam);
        }

        /* ---------- Helpers ------------------------------------------------ */

        private static bool HasCollider(Node3D n) =>
            n.Components.Any(c => c.Enabled && c is ColliderComponent);

        private static BoundingBox? ToAABB(BoundingVolume v) => v switch
        {
            BoundingBox bb => bb,
            BoundingSphere s => BoundingBox.CreateFromMinMax(
                                    s.Center - new Vector3(s.Radius),
                                    s.Center + new Vector3(s.Radius)),
            BoundingBoxOBB o => BoundingBox.CreateFromMinMax(
                                    o.GetCorners().ComponentMin(),
                                    o.GetCorners().ComponentMax()),
            _ => null
        };

        private static BoundingBox? Union(BoundingBox? a, BoundingBox? b)
        {
            if (a == null) return b;
            if (b == null) return a;
            a.Expand(b);
            return a;
        }
    }

    static class Vector3ArrayExt
    {
        public static Vector3 ComponentMin(this Vector3[] a)
        {
            var m = a[0];
            for (int i = 1; i < a.Length; i++) m = Vector3.ComponentMin(m, a[i]);
            return m;
        }

        public static Vector3 ComponentMax(this Vector3[] a)
        {
            var m = a[0];
            for (int i = 1; i < a.Length; i++) m = Vector3.ComponentMax(m, a[i]);
            return m;
        }
    }
}
