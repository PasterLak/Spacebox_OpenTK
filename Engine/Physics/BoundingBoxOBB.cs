using System;
using OpenTK.Mathematics;

namespace Engine.Physics
{
    public class BoundingBoxOBB : BoundingVolume
    {
        public Vector3 Size { get; set; }
        public Quaternion Rotation { get; set; }
        // Half of the size along each axis
        public Vector3 Extents => Size * 0.5f;

        public BoundingBoxOBB(BoundingBoxOBB obb)
        {
            Center = obb.Center;
            Size = obb.Size;
            Rotation = obb.Rotation;
        }

        public BoundingBoxOBB(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Center = center;
            Size = size;
            Rotation = rotation;
        }

        /// <summary>
        /// Computes the 8 corner points of the OBB.
        /// </summary>
        public Vector3[] GetCorners()
        {
            Vector3[] corners = new Vector3[8];
            Vector3 extents = Extents;
          
            Vector3[] localCorners = new Vector3[8];
            localCorners[0] = new Vector3(-extents.X, -extents.Y, -extents.Z);
            localCorners[1] = new Vector3(extents.X, -extents.Y, -extents.Z);
            localCorners[2] = new Vector3(-extents.X, extents.Y, -extents.Z);
            localCorners[3] = new Vector3(extents.X, extents.Y, -extents.Z);
            localCorners[4] = new Vector3(-extents.X, -extents.Y, extents.Z);
            localCorners[5] = new Vector3(extents.X, -extents.Y, extents.Z);
            localCorners[6] = new Vector3(-extents.X, extents.Y, extents.Z);
            localCorners[7] = new Vector3(extents.X, extents.Y, extents.Z);

      
            for (int i = 0; i < 8; i++)
            {
                corners[i] = Center + Vector3.Transform(localCorners[i], Rotation);
            }

            return corners;
        }

        /// <summary>
        /// Checks for intersection with another BoundingVolume.
        /// Supports BoundingBoxOBB, AABB (as OBB with zero rotation) and BoundingSphere.
        /// </summary>
        public override bool Intersects(BoundingVolume other)
        {
            if (other is BoundingBoxOBB obb)
            {
                return IntersectsOBB(obb);
            }
            else if (other is BoundingBox aabb)
            {
           
                BoundingBoxOBB aabbAsOBB = new BoundingBoxOBB(aabb.Center, aabb.Size, Quaternion.Identity);
                return IntersectsOBB(aabbAsOBB);
            }
            else if (other is BoundingSphere sphere)
            {
                return IntersectsSphere(sphere);
            }
            return false;
        }

        /// <summary>
        /// Uses the Separating Axis Theorem (SAT) to check for intersection between two OBBs.
        /// </summary>
        private bool IntersectsOBB(BoundingBoxOBB obb)
        {
        
            Vector3[] A = new Vector3[3];
            A[0] = Vector3.Transform(Vector3.UnitX, Rotation);
            A[1] = Vector3.Transform(Vector3.UnitY, Rotation);
            A[2] = Vector3.Transform(Vector3.UnitZ, Rotation);

            Vector3[] B = new Vector3[3];
            B[0] = Vector3.Transform(Vector3.UnitX, obb.Rotation);
            B[1] = Vector3.Transform(Vector3.UnitY, obb.Rotation);
            B[2] = Vector3.Transform(Vector3.UnitZ, obb.Rotation);

        
            float[,] R = new float[3, 3];
            float[,] AbsR = new float[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    R[i, j] = Vector3.Dot(A[i], B[j]);
                    AbsR[i, j] = MathF.Abs(R[i, j]) + 1e-6f;
                }
            }

         
            Vector3 t = obb.Center - Center;
            Vector3 tA = new Vector3(Vector3.Dot(t, A[0]), Vector3.Dot(t, A[1]), Vector3.Dot(t, A[2]));

            Vector3 aExtents = Extents;
            Vector3 bExtents = obb.Extents;

            for (int i = 0; i < 3; i++)
            {
                float ra = aExtents[i];
                float rb = bExtents.X * AbsR[i, 0] + bExtents.Y * AbsR[i, 1] + bExtents.Z * AbsR[i, 2];
                if (MathF.Abs(tA[i]) > ra + rb)
                    return false;
            }

    
            for (int j = 0; j < 3; j++)
            {
                float ra = aExtents.X * AbsR[0, j] + aExtents.Y * AbsR[1, j] + aExtents.Z * AbsR[2, j];
                float rb = bExtents[j];
                float tVal = MathF.Abs(tA.X * R[0, j] + tA.Y * R[1, j] + tA.Z * R[2, j]);
                if (tVal > ra + rb)
                    return false;
            }


            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float ra = aExtents[(i + 1) % 3] * AbsR[(i + 2) % 3, j] + aExtents[(i + 2) % 3] * AbsR[(i + 1) % 3, j];
                    float rb = bExtents[(j + 1) % 3] * AbsR[i, (j + 2) % 3] + bExtents[(j + 2) % 3] * AbsR[i, (j + 1) % 3];
                    float tVal = MathF.Abs(tA[(i + 2) % 3] * R[(i + 1) % 3, j] - tA[(i + 1) % 3] * R[(i + 2) % 3, j]);
                    if (tVal > ra + rb)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks for intersection with a BoundingSphere.
        /// Transforms the sphere center into the OBB's local space and clamps it to the OBB extents.
        /// </summary>
        private bool IntersectsSphere(BoundingSphere sphere)
        {
        
            Quaternion invRotation = Quaternion.Invert(Rotation);
            Vector3 localCenter = Vector3.Transform(sphere.Center - Center, invRotation);
            Vector3 halfSize = Extents;

            Vector3 clamped = new Vector3(
                MathF.Max(-halfSize.X, MathF.Min(localCenter.X, halfSize.X)),
                MathF.Max(-halfSize.Y, MathF.Min(localCenter.Y, halfSize.Y)),
                MathF.Max(-halfSize.Z, MathF.Min(localCenter.Z, halfSize.Z))
            );

            Vector3 diff = localCenter - clamped;
            return diff.LengthSquared <= sphere.Radius * sphere.Radius;
        }

        /// <summary>
        /// Returns the longest side length of the OBB.
        /// </summary>
        public override float GetLongestSide()
        {
            return MathHelper.Max(MathHelper.Max(Size.X, Size.Y), Size.Z);
        }

        public override BoundingVolume Clone()
        {
            return new BoundingBoxOBB(Center, Size, Rotation);
        }

        public override string ToString()
        {
            return $"OBB Center: {Center}, Size: {Size}, Rotation: {Rotation}";
        }
    }
}
