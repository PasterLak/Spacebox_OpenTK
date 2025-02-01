using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Resources;
using static Spacebox.Game.ItemModelGeneratorHelper;

namespace Spacebox.Game
{
    public static class ItemModelGenerator
    {
        private static int CellSize = 32;

            public static ItemModel GenerateModel(Texture2D atlasTexture,
            int cellX, int cellY, float modelSize = 1.0f, float modelDepth = 0.2f, bool isAnimated = false, bool drawOnlyVisibleSides = true)
        {
            Texture2D cellTexture = UVAtlas.GetBlockTexture(atlasTexture, cellX, cellY, GameBlocks.AtlasItems.SizeBlocks);
            cellTexture.FlipX();
            CellSize = cellTexture.Width;

            Color4[,] pixels = cellTexture.GetPixelData();

            int width = cellTexture.Width;
            int height = cellTexture.Height;

            var quads = ItemModelGeneratorHelper.GreedyMesh(pixels, width, height);


            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint indexOffset = 0;


            foreach (var quad in quads)
            {
                Vector3 bottomLeft = new Vector3(quad.X, quad.Y, 0);
                Vector3 bottomRight = new Vector3((quad.X + quad.Width), quad.Y, 0);
                Vector3 topRight = new Vector3((quad.X + quad.Width), (quad.Y + quad.Height), 0);
                Vector3 topLeft = new Vector3(quad.X, (quad.Y + quad.Height), 0);

                bottomLeft = bottomLeft * modelSize;
                bottomRight = bottomRight * modelSize;
                topRight = topRight * modelSize;
                topLeft = topLeft * modelSize;

                var uv = GetUVs(quad);
                Vector2 uv1 = new Vector2(quad.U, quad.V);
                Vector2 uv2 = new Vector2(quad.U + quad.UWidth, quad.V);
                Vector2 uv3 = new Vector2(quad.U + quad.UWidth, quad.V + quad.UHeight);
                Vector2 uv4 = new Vector2(quad.U, quad.V + quad.UHeight);


                AddFace(vertices, indices, indexOffset,
                     topLeft, topRight, bottomRight, bottomLeft,
                     new Vector3(1.0f, 1.0f, 1.0f), uv[0], uv[1], uv[2], uv[3]);
                indexOffset += 4;


                if (quad.NeedsTopSide && !drawOnlyVisibleSides)
                {
                    ItemModelGeneratorHelper.AddFaceButton(vertices, indices, indexOffset,
                        quad, modelDepth, modelSize,
                   uv[0], uv[1], uv[2], uv[3]);

                    indexOffset += 4;
                }

                if (quad.NeedsBottomSide)
                {
                    ItemModelGeneratorHelper.AddFaceTop(vertices, indices, indexOffset,
                        quad, modelDepth, modelSize,
                    uv[0], uv[1], uv[2], uv[3]);

                    indexOffset += 4;
                }

                if (quad.NeedsRightSide && !drawOnlyVisibleSides)
                {
                    ItemModelGeneratorHelper.AddFaceForward(vertices, indices, indexOffset,
                        quad, modelDepth, modelSize,
                     uv[0], uv[1], uv[2], uv[3]);

                    indexOffset += 4;
                }
                if (quad.NeedsLeftSide) // ++
                {
                    ItemModelGeneratorHelper.AddFaceBack
                        (vertices, indices, indexOffset,
                        quad, modelDepth, modelSize,
                     uv[0], uv[1], uv[2], uv[3]);

                    indexOffset += 4;
                }



            }

            if (!drawOnlyVisibleSides)
            {
                foreach (var quad in quads)
                {
                    Vector3 bottomLeft = new Vector3(quad.X, quad.Y, 0);
                    Vector3 bottomRight = new Vector3((quad.X + quad.Width), quad.Y, 0);
                    Vector3 topRight = new Vector3((quad.X + quad.Width), (quad.Y + quad.Height), 0);
                    Vector3 topLeft = new Vector3(quad.X, (quad.Y + quad.Height), 0);


                    bottomLeft = bottomLeft * modelSize;
                    bottomRight = bottomRight * modelSize;
                    topRight = topRight * modelSize;
                    topLeft = topLeft * modelSize;

                    bottomLeft = bottomLeft + new Vector3(0, 0, modelDepth);
                    bottomRight = bottomRight + new Vector3(0, 0, modelDepth);
                    topRight = topRight + new Vector3(0, 0, modelDepth);
                    topLeft = topLeft + new Vector3(0, 0, modelDepth);


                    var uv = GetUVs(quad);

                    Vector2 uv1 = new Vector2(quad.U, quad.V);
                    Vector2 uv2 = new Vector2(quad.U + quad.UWidth, quad.V);
                    Vector2 uv3 = new Vector2(quad.U + quad.UWidth, quad.V + quad.UHeight);
                    Vector2 uv4 = new Vector2(quad.U, quad.V + quad.UHeight);



                    ItemModelGeneratorHelper.AddFace(vertices, indices, indexOffset,
                         topLeft, bottomLeft, bottomRight, topRight,
                         new Vector3(1.0f, 1.0f, 1.0f), uv[0], uv[1], uv[2], uv[3]);

                    indexOffset += 4;
                }
            }


            float[] vertexArray = vertices.ToArray();
            uint[] indexArray = indices.ToArray();
            Mesh mesh = new Mesh(vertexArray, indexArray, BuffersData.CreateItemModelBuffer());

            if(!isAnimated)
                return new ItemModel(mesh, cellTexture);
            else
                return new AnimatedItemModel(mesh, cellTexture);
        }

        public static Vector2[] GetUVs(ItemModelGeneratorHelper.Quad quad)
        {
            float pixelUV = 1f / CellSize;

            Vector2 uvLeft = new Vector2(quad.X * pixelUV, quad.Y * pixelUV);
            Vector2 uvRight = new Vector2((quad.X + quad.Width) * pixelUV, quad.Y * pixelUV);
            Vector2 uvTopRight = new Vector2((quad.X + quad.Width) * pixelUV, (quad.Y + quad.Height) * pixelUV);
            Vector2 uvTop = new Vector2(quad.X * pixelUV, (quad.Y + quad.Height) * pixelUV);

            return new Vector2[] { uvLeft, uvRight, uvTopRight, uvTop };


        }


    }
}
