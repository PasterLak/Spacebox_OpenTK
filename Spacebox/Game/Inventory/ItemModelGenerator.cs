
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game
{
    public static class ItemModelGenerator
    {
        public static ItemModel GenerateItemModel(
            Texture2D cellTexture,
            Texture2D cellTexture2,
            float modelSize = 1f,
            float modelDepth = 0.2f,
            bool isAnimated = false,
            bool drawOnlyVisibleSides = true)
        {

            cellTexture.Name = "ItemModel";
            cellTexture2.Name = "ItemModel_emission";
            Resources.AddResourceToDispose(cellTexture);
            Resources.AddResourceToDispose(cellTexture2);

            if (cellTexture2.XWasFlipped == false)
                cellTexture2.FlipX();
            var mesh = BuildItemMesh(cellTexture, modelSize, modelDepth, drawOnlyVisibleSides);
            return ItemModelFromMesh(cellTexture, cellTexture2, mesh, isAnimated);
        }

        public static Model GenerateWorldModel(
            Texture2D cellTexture,
            float modelDepth = 0.2f
         )
        {
            //var cellTexture2 = UVAtlas.GetBlockTexture(emissionAtlas, cellX, cellY, GameAssets.AtlasItems.SizeBlocks);

            cellTexture.Name = "ItemWorldModel";
            Resources.AddResourceToDispose(cellTexture);

            //cellTexture2.FlipX();
            var mesh = BuildItemMesh(cellTexture, 1f / cellTexture.Width, modelDepth, false);
            return new Model(mesh, new TextureMaterial(cellTexture));
        }

        public static ItemModel GenerateModelFromTexture(
            Texture2D texture,
            Texture2D emission,
            float modelSize = 1f,
            float modelDepth = 0.2f,
            bool isAnimated = false,
            bool drawOnlyVisibleSides = true)
        {
            var mesh = BuildItemMesh(texture, modelSize, modelDepth, drawOnlyVisibleSides);
            return ItemModelFromMesh(texture, emission, mesh, isAnimated);
        }

        public static Mesh GenerateMeshFromTexture(
            Texture2D texture,
          
            float modelDepth = 0.2f,
            bool drawOnlyVisibleSides = false)
        {
            return BuildItemMesh(texture, 1f / texture.Width, modelDepth, drawOnlyVisibleSides);
        }

        private static ItemModel ItemModelFromMesh(Texture2D cellTexture,Texture2D emission, Mesh mesh, bool isAnimated)
        {
            return isAnimated ? new AnimatedItemModel(mesh, cellTexture, emission) : new ItemModel(mesh, cellTexture, emission);
        }

        private static Mesh BuildItemMesh(
            Texture2D cellTexture,
          
            float modelSize,
            float modelDepth,
            bool drawOnlyVisibleSides)
        {
            if(!cellTexture.XWasFlipped)
                cellTexture.FlipX();
          
            var solid = ItemMeshBuilder.SolidMask(cellTexture.GetPixels());
            var (vertices, indices) = ItemMeshBuilder.Build(solid, modelSize, modelDepth, drawOnlyVisibleSides);

            return new Mesh(vertices, indices, BuffersData.CreateItemModelBuffer());
        }
    }
}
