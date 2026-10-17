using Client;
using Engine;
using Spacebox.Game;
using Spacebox.Game.Resource;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{

    public static class SceneAssetsPreloader
    {

        private static void InitializeGamesetData(string blocksPath, string itemsPath, string emissionPath, string modId, byte blockSizePixels, string serverName, bool useLocalGameSetsFolder, bool isMultiplayer)
        {

            GameAssets.AtlasBlocks = new AtlasTexture();
            GameAssets.AtlasItems = new AtlasTexture();
            var texture = GameAssets.AtlasBlocks.CreateTexture(blocksPath, blockSizePixels, false);


            var items = GameAssets.AtlasItems.CreateTexture(itemsPath, blockSizePixels, false);
            var emissions = GameAssets.AtlasBlocks.CreateEmission(emissionPath);
            var emissions2 = GameAssets.AtlasItems.CreateEmission(emissionPath);
           
            GameAssets.BlocksTexture = texture;
            GameAssets.ItemsTexture = items;
            GameAssets.EmissionBlocks = emissions;
            GameAssets.EmissionItems = emissions2;


            GameSetLoader.Load(modId, useLocalGameSetsFolder, isMultiplayer ? serverName : "");


            GameAssets.IsInitialized = true;
        }

        public static void Preload(SpaceSceneArgs param, BaseSpaceScene scene, Astronaut astronaut)
        {

            bool isMultiplayer = false;
            if ((scene as MultiplayerScene) != null)
            {
                isMultiplayer = true;
                param.SearchForGameSetInLocalFolder = true;
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


            string modsFolder = ModPath.GetModsPath(param.SearchForGameSetInLocalFolder, serverName);

            string blocksPath = ModPath.GetBlocksPath(modsFolder, modFolderName);

            string itemsPath = ModPath.GetItemsPath(modsFolder, modFolderName);
            string emissionPath = ModPath.GetEmissionsPath(modsFolder, modFolderName);

            if (GameAssets.IsInitialized)
            {
                
                    GameAssets.DisposeAll();
                    InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName, param.SearchForGameSetInLocalFolder, isMultiplayer);
                
            }
            else
            {
                InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName, param.SearchForGameSetInLocalFolder,isMultiplayer);
              
            }

        }
    }
}
