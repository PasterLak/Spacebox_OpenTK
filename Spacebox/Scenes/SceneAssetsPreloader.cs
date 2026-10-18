using Spacebox.Network;
using Engine;
using Spacebox.Game;
using Spacebox.Game.Resource;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{

    public static class SceneAssetsPreloader
    {

        private static void InitializeGamesetData(string blocksPath, string itemsPath, string emissionPath, string modId, string serverName, bool useLocalGameSetsFolder, bool isMultiplayer)
        {

            GameAssets.AtlasBlocks = GameAtlasFactory.CreateBlocksAtlas(blocksPath, emissionPath);
            GameAssets.AtlasItems = GameAtlasFactory.CreateItemsAtlas(itemsPath, emissionPath);


            GameSetLoader.Load(modId, useLocalGameSetsFolder, isMultiplayer ? serverName : "");


            GameAssets.IsInitialized = true;
        }

        public static void Preload(SpaceSceneArgs param, BaseSpaceScene scene, Astronaut astronaut)
        {

            bool isMultiplayer = !param.SearchForGameSetInLocalFolder;
            var modId = param.modId;
            var modFolderName = string.IsNullOrEmpty(param.modfolderName) ? GameSetLocator.FindFolder(modId) : param.modfolderName;
            string serverName = isMultiplayer ? param.gameSetServerFolder ?? "" : "";


            string modsFolder = ModPath.GetModsPath(param.SearchForGameSetInLocalFolder, serverName);

            string blocksPath = ModPath.GetBlocksPath(modsFolder, modFolderName);

            string itemsPath = ModPath.GetItemsPath(modsFolder, modFolderName);
            string emissionPath = ModPath.GetEmissionsPath(modsFolder, modFolderName);

            if (GameAssets.IsInitialized)
            {
                
                    GameAssets.DisposeAll();
                    InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, serverName, param.SearchForGameSetInLocalFolder, isMultiplayer);
                
            }
            else
            {
                InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, serverName, param.SearchForGameSetInLocalFolder, isMultiplayer);
              
            }

        }
    }
}
