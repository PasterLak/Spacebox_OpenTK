
using Client;
using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Game;
using Spacebox.GUI;
using Spacebox.Game.Resource;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{

    public static class SceneAssetsPreloader
    {

        private static void InitializeGamesetData(string blocksPath, string itemsPath, string emissionPath, string modId, byte blockSizePixels, string serverName, bool isMultiplayer)
        {

            GameAssets.AtlasBlocks = new AtlasTexture();
            GameAssets.AtlasItems = new AtlasTexture();
            var texture = GameAssets.AtlasBlocks.CreateTexture(blocksPath, blockSizePixels, false);

            //var blockIcons = GameAssets.AtlasBlocks.GetTextureDatas(texture);

            /*for (int i = 0; i < blockIcons.Length; i++)
            {
               
                blockIcons[i].Name = blockIcons[i].Name + "_block";
            }*/

           // Debug.Error("count : " + blockIcons.Length);

            var items = GameAssets.AtlasItems.CreateTexture(itemsPath, blockSizePixels, false);
            var emissions = GameAssets.AtlasBlocks.CreateEmission(emissionPath);
            GameAssets.BlocksTexture = texture;
            GameAssets.ItemsTexture = items;
            GameAssets.LightAtlas = emissions;

            

            GameSetLoader.Load(modId, isMultiplayer, isMultiplayer ? serverName : "");


           /* var blockIcons = new List<AtlasTexture.TextureData>();

            foreach (var item in GameAssets.Items.Values)
            {
                if (item.Is<BlockItem>())
                {
                    blockIcons.Add(new AtlasTexture.TextureData()
                    {
                        Name = item.Name + "_block",
                        Texture = GameAssets.ItemIcons[item.Id],
                    });
                }
            }


            items.Dispose();
            GameAssets.AtlasItems.Dispose();
            GameAssets.AtlasItems = new AtlasTexture();
            var items2 = GameAssets.AtlasItems.CreateTexture(itemsPath, blockIcons.ToArray(), blockSizePixels, false);
            GameAssets.ItemsTexture = items2;
            items2.SaveToPng("items.png");*/

            GameAssets.IsInitialized = true;
        }

        public static void Preload(SpaceSceneArgs param, BaseSpaceScene scene, Astronaut astronaut)
        {

            bool isMultiplayer = false;
            if ((scene as MultiplayerScene) != null)
            {
                isMultiplayer = true;
            }

            var modFolderName = "";
            var modId = param.modId;
            var seedString = param.seed;

            if (isMultiplayer)
            {
                if (ClientNetwork.Instance != null)
                {
                    modFolderName = ClientNetwork.Instance.ReceivedServerInfo.ModFolderName;
                }
            }
            else
            {
                modFolderName = param.modfolderName;
            }

            string serverName = "";

            if (isMultiplayer)
            {
                serverName = param.worldName;
            }


            string modsFolder = ModPath.GetModsPath(isMultiplayer, serverName);

            string blocksPath = ModPath.GetBlocksPath(modsFolder, modFolderName);

            string itemsPath = ModPath.GetItemsPath(modsFolder, modFolderName);
            string emissionPath = ModPath.GetEmissionsPath(modsFolder, modFolderName);

            if (GameAssets.IsInitialized)
            {
                if (GameAssets.ModId.ToLower() != modId.ToLower())
                {
                    GameAssets.DisposeAll();
                    InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName, isMultiplayer);
                }
            }
            else
            {
                InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName, isMultiplayer);
            }
            if (int.TryParse(seedString, out var seed))
            {
                //World.Random = new Random(seed);
            }
            else
            {
                // World.Random = new Random();
                Debug.Error("Wrong seed format! Seed: " + seedString);
            }


        }
    }
}
