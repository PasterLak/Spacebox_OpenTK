using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Engine.Utils
{
    public static class GenMesh
    {
        public static Mesh CreateQuad()
        {
            float[] v = {
                -0.5f,-0.5f,0, 0,0,1, 0,0,
                 0.5f,-0.5f,0, 0,0,1, 1,0,
                 0.5f, 0.5f,0, 0,0,1, 1,1,
                -0.5f, 0.5f,0, 0,0,1, 0,1
            };
            int[] i = { 0, 1, 2, 2, 3, 0 };
            var r =  new Mesh(v, i);
            Resources.AddResourceToDispose(r);
            return r;
        }

        public static Mesh CreatePlane(int xSegments = 1, int zSegments = 1)
        {
            List<float> verts = new();
            List<int> idx = new();
            for (int z = 0; z <= zSegments; z++)
            {
                for (int x = 0; x <= xSegments; x++)
                {
                    float xf = (float)x / xSegments - 0.5f;
                    float zf = (float)z / zSegments - 0.5f;
                    verts.AddRange(new[] { xf, 0, zf, 0, 1, 0, (float)x / xSegments, (float)z / zSegments });
                    if (x < xSegments && z < zSegments)
                    {
                        int start = z * (xSegments + 1) + x;
                        idx.AddRange(new[] { start, start + xSegments + 1, start + 1, start + 1, start + xSegments + 1, start + xSegments + 2 });
                    }
                }
            }
          
            var r = new Mesh(verts.ToArray(), idx.ToArray());
            Resources.AddResourceToDispose(r);
            return r;
        }

        public static Mesh CreateSphere(int rings = 16)
        {
            int stacks = rings;
            int sectors = rings * 2;
            List<float> verts = new();
            List<int> idx = new();
            for (int i = 0; i <= stacks; i++)
            {
                float v = (float)i / stacks;
                float phi = MathF.PI * v;
                for (int j = 0; j <= sectors; j++)
                {
                    float u = (float)j / sectors;
                    float theta = 2f * MathF.PI * u;
                    float x = MathF.Sin(phi) * MathF.Cos(theta);
                    float y = MathF.Cos(phi);
                    float z = MathF.Sin(phi) * MathF.Sin(theta);
                    verts.AddRange(new[] { x * 0.5f, y * 0.5f, z * 0.5f, x, y, z, u, v });
                    if (i < stacks && j < sectors)
                    {
                        int cur = i * (sectors + 1) + j;
                        int next = cur + sectors + 1;
                        idx.AddRange(new[] { cur, next, cur + 1, cur + 1, next, next + 1 });
                    }
                }
            }

            var r = new Mesh(verts.ToArray(), idx.ToArray());
            Resources.AddResourceToDispose(r);
            return r;
        }


        public static Mesh CreateTiledPlane(int tilesX, int tilesZ)
        {
            tilesX = Math.Max(1, tilesX);
            tilesZ = Math.Max(1, tilesZ);

            int maxT = Math.Max(tilesX, tilesZ);
            float width = tilesX / (float)maxT;
            float depth = tilesZ / (float)maxT;

            float stepX = width / tilesX;
            float stepZ = depth / tilesZ;
            float x0 = -width * 0.5f;
            float z0 = -depth * 0.5f;

            List<float> verts = new();
            List<uint> idx = new();

            for (int z = 0; z <= tilesZ; ++z)
            {
                for (int x = 0; x <= tilesX; ++x)
                {
                    float px = x0 + x * stepX;
                    float pz = z0 + z * stepZ;

                    verts.AddRange(new[]
                    {
                px, 0f, pz,
                0f, 1f, 0f,
                (float)x / tilesX,
                (float)z / tilesZ
            });

                    if (x < tilesX && z < tilesZ)
                    {
                        uint start = (uint)(z * (tilesX + 1) + x);
                        idx.AddRange(new[]
                        {
                    start,
                    (uint)(start + tilesX + 1),
                    (uint)(start + 1),
                    (uint)(start + 1),
                    (uint)(start + tilesX + 1),
                    (uint)(start + tilesX + 2)
                });
                    }
                }
            }

            var mesh = new Mesh(verts.ToArray(), idx.ToArray(), TextureMaterial.GetMeshBuffer());
            Resources.AddResourceToDispose(mesh);
            return mesh;
        }


        public static Mesh CreateCube()
        {
            float[] v = {
                -0.5f,-0.5f, 0.5f, 0,0,1, 0,0,
                 0.5f,-0.5f, 0.5f, 0,0,1, 1,0,
                 0.5f, 0.5f, 0.5f, 0,0,1, 1,1,
                -0.5f, 0.5f, 0.5f, 0,0,1, 0,1,

                 0.5f,-0.5f,-0.5f, 0,0,-1, 0,0,
                -0.5f,-0.5f,-0.5f, 0,0,-1, 1,0,
                -0.5f, 0.5f,-0.5f, 0,0,-1, 1,1,
                 0.5f, 0.5f,-0.5f, 0,0,-1, 0,1,

                -0.5f,-0.5f,-0.5f,-1,0,0, 0,0,
                -0.5f,-0.5f, 0.5f,-1,0,0, 1,0,
                -0.5f, 0.5f, 0.5f,-1,0,0, 1,1,
                -0.5f, 0.5f,-0.5f,-1,0,0, 0,1,

                 0.5f,-0.5f, 0.5f, 1,0,0, 0,0,
                 0.5f,-0.5f,-0.5f, 1,0,0, 1,0,
                 0.5f, 0.5f,-0.5f, 1,0,0, 1,1,
                 0.5f, 0.5f, 0.5f, 1,0,0, 0,1,

                -0.5f, 0.5f, 0.5f, 0,1,0, 0,0,
                 0.5f, 0.5f, 0.5f, 0,1,0, 1,0,
                 0.5f, 0.5f,-0.5f, 0,1,0, 1,1,
                -0.5f, 0.5f,-0.5f, 0,1,0, 0,1,

                -0.5f,-0.5f,-0.5f, 0,-1,0, 0,0,
                 0.5f,-0.5f,-0.5f, 0,-1,0, 1,0,
                 0.5f,-0.5f, 0.5f, 0,-1,0, 1,1,
                -0.5f,-0.5f, 0.5f, 0,-1,0, 0,1
            };
            int[] i = {
                 0, 1, 2, 2, 3, 0,
                 4, 5, 6, 6, 7, 4,
                 8, 9,10,10,11, 8,
                12,13,14,14,15,12,
                16,17,18,18,19,16,
                20,21,22,22,23,20
            };
         
            var r = new Mesh(v, i);
            Resources.AddResourceToDispose(r);
            return r;
        }


        public static Mesh CreateCone(
     float angleDeg,
     float length,
     Vector3 direction,
     Vector3 eulerAngles,
     int segments = 24)
        {
            float radius = MathF.Tan(MathHelper.DegreesToRadians(angleDeg)) * length;

            Vector3 dirNorm = direction.Normalized();
            Vector3 axis = Vector3.Cross(Vector3.UnitZ, dirNorm);
            float dot = Vector3.Dot(Vector3.UnitZ, dirNorm);
            float clamped = MathF.Max(-1f, MathF.Min(1f, dot));
            float alignAngle = MathF.Acos(clamped);
            Quaternion rotAlign = Quaternion.FromAxisAngle(
                axis.LengthFast > 1e-6f ? axis.Normalized() : Vector3.UnitX,
                alignAngle);

            Quaternion rotEuler = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(eulerAngles.X),
                MathHelper.DegreesToRadians(eulerAngles.Y),
                MathHelper.DegreesToRadians(eulerAngles.Z));

            Quaternion rot = rotEuler * rotAlign;

            var verts = new List<float>();
            var idx = new List<int>();

            var p0 = rot * new Vector3(0, 0, 0);
            var n0 = rot * Vector3.UnitZ;
            verts.AddRange(new[] { p0.X, p0.Y, p0.Z, n0.X, n0.Y, n0.Z, 0.5f, 1f });

            var p1 = rot * new Vector3(0, 0, length);
            var n1 = rot * -Vector3.UnitZ;
            verts.AddRange(new[] { p1.X, p1.Y, p1.Z, n1.X, n1.Y, n1.Z, 0.5f, 0f });

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float theta = MathHelper.TwoPi * t;
                var lp = new Vector3(
                    radius * MathF.Cos(theta),
                    radius * MathF.Sin(theta),
                    length);
                var wp = rot * lp;

                var snLocal = new Vector3(lp.X, lp.Y, radius / length);
                var snWorld = rot * Vector3.Normalize(snLocal);

                verts.AddRange(new[]
                {
            wp.X, wp.Y, wp.Z,
            snWorld.X, snWorld.Y, snWorld.Z,
            t, 0f
        });
            }

            int apexIndex = 0, firstRing = 2;
            for (int i = 0; i < segments; i++)
            {
                idx.Add(apexIndex);
                idx.Add(firstRing + i + 1);
                idx.Add(firstRing + i);
            }

            int baseCenterIndex = 1;
            for (int i = 0; i < segments; i++)
            {
                idx.Add(baseCenterIndex);
                idx.Add(firstRing + i);
                idx.Add(firstRing + i + 1);
            }

            var mesh = new Mesh(verts.ToArray(), idx.ToArray());
            Resources.AddResourceToDispose(mesh);
            return mesh;
        }



    }
}
