using System.Collections.Generic;
using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Resource;
using static Spacebox.Game.ItemModelGeneratorHelper;

namespace Spacebox.Game
{
    public static class ItemModelGenerator
    {
        private static int CellSize = 32;

        public static ItemModel GenerateModelFromAtlas(
            Texture2D atlasTexture,
            int cellX,
            int cellY,
            float modelSize = 1f,
            float modelDepth = 0.2f,
            bool isAnimated = false,
            bool drawOnlyVisibleSides = true)
        {
            CellSize = 32;
            var cellTexture = UVAtlas.GetBlockTexture(atlasTexture, cellX, cellY, GameAssets.AtlasItems.SizeBlocks);
            var mesh = BuildItemModel(cellTexture, modelSize, modelDepth, drawOnlyVisibleSides);
            return ItemModelFromMesh(cellTexture, mesh, isAnimated);
        }

        public static ItemModel GenerateModelFromTexture(
            Texture2D texture,
            float modelSize = 1f,
            float modelDepth = 0.2f,
            bool isAnimated = false,
            bool drawOnlyVisibleSides = true)
        {
            CellSize = 1;
            var mesh = BuildItemModel(texture, modelSize, modelDepth, drawOnlyVisibleSides);
            return ItemModelFromMesh(texture, mesh, isAnimated);
        }
        public static Mesh GenerateMeshFromTexture(
            Texture2D texture,
            float modelDepth = 0.2f,
            bool drawOnlyVisibleSides = false)
        {
            CellSize = 1;
            return BuildItemModel(texture, 1f/texture.Width, modelDepth, drawOnlyVisibleSides);
            
        }

        private static ItemModel ItemModelFromMesh(Texture2D cellTexture,
          Mesh mesh,
            bool isAnimated
          )
        {
            return isAnimated ? new AnimatedItemModel(mesh, cellTexture) : new ItemModel(mesh, cellTexture);
        }

        private static Mesh BuildItemModel(
            Texture2D cellTexture,
            float modelSize,
            float modelDepth,
            bool drawOnlyVisibleSides)
        {
            cellTexture.FlipX();
            CellSize = cellTexture.Width;

            var pixels = cellTexture.GetPixelData();
            var quads = GreedyMesh(pixels, cellTexture.Width, cellTexture.Height);

            List<float> vertices = new();
            List<uint>  indices  = new();
            uint indexOffset = 0;

            foreach (var q in quads)
            {
                Vector3 bl = new Vector3(q.X,                 q.Y,                 0) * modelSize;
                Vector3 br = new Vector3(q.X + q.Width,       q.Y,                 0) * modelSize;
                Vector3 tr = new Vector3(q.X + q.Width,       q.Y + q.Height,      0) * modelSize;
                Vector3 tl = new Vector3(q.X,                 q.Y + q.Height,      0) * modelSize;

                var uv = GetUVs(q);

                AddFace(vertices, indices, indexOffset, tl, tr, br, bl, Vector3.One, uv[0], uv[1], uv[2], uv[3]);
                indexOffset += 4;

                if (q.NeedsTopSide && !drawOnlyVisibleSides)
                {
                    AddFaceButton(vertices, indices, indexOffset, q, modelDepth, modelSize, uv[0], uv[1], uv[2], uv[3]);
                    indexOffset += 4;
                }

                if (q.NeedsBottomSide)
                {
                    AddFaceTop(vertices, indices, indexOffset, q, modelDepth, modelSize, uv[0], uv[1], uv[2], uv[3]);
                    indexOffset += 4;
                }

                if (q.NeedsRightSide && !drawOnlyVisibleSides)
                {
                    AddFaceForward(vertices, indices, indexOffset, q, modelDepth, modelSize, uv[0], uv[1], uv[2], uv[3]);
                    indexOffset += 4;
                }

                if (q.NeedsLeftSide)
                {
                    AddFaceBack(vertices, indices, indexOffset, q, modelDepth, modelSize, uv[0], uv[1], uv[2], uv[3]);
                    indexOffset += 4;
                }
            }

            if (!drawOnlyVisibleSides) // draw right side
            {
                var offset = cellTexture.Width / 2f;
                foreach (var q in quads)
                {
                    
                    Vector3 bl = new Vector3(q.X,                 q.Y,                 offset) * modelSize;
                    Vector3 br = new Vector3(q.X + q.Width,       q.Y,                 offset) * modelSize;
                    Vector3 tr = new Vector3(q.X + q.Width,       q.Y + q.Height,      offset) * modelSize;
                    Vector3 tl = new Vector3(q.X,                 q.Y + q.Height,      offset) * modelSize;

                    var uv = GetUVs(q);

                    AddFace(vertices, indices, indexOffset, tl, bl, br, tr, Vector3.One, uv[0], uv[1], uv[2], uv[3]);
                    indexOffset += 4;
                }
            }

            return  new Mesh(vertices.ToArray(), indices.ToArray(), BuffersData.CreateItemModelBuffer());
          
        }

        public static Vector2[] GetUVs(Quad quad)
        {
            float pixelUV = 1f / CellSize;
            Vector2 uvLeft     = new(quad.X * pixelUV,                 quad.Y * pixelUV);
            Vector2 uvRight    = new((quad.X + quad.Width) * pixelUV,  quad.Y * pixelUV);
            Vector2 uvTopRight = new((quad.X + quad.Width) * pixelUV,  (quad.Y + quad.Height) * pixelUV);
            Vector2 uvTop      = new(quad.X * pixelUV,                 (quad.Y + quad.Height) * pixelUV);
            return new[] { uvLeft, uvRight, uvTopRight, uvTop };
        }
    }
}
