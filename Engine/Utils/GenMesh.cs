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
    }
}
