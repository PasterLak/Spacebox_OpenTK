using Spacebox.Network;
using Engine;
using Spacebox.Game;
using Spacebox.Game.Resource;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Player;

namespace Spacebox.Scenes
{

    public static class SceneAssetsPreloader
    {
        private static void InitializeGamesetData(GameSetStack stack, bool applyTexturePacks)
        {
            var layers = stack?.AssetLayers ?? Array.Empty<ContentLayer>();
            var overrides = applyTexturePacks ? Spacebox.Game.Resource.TexturePacks.TexturePackService.ActiveOverrides() : null;
            var assets = GameSetAssetIndex.Build(layers, GameAtlasFactory.BuiltInBlocksFolder, GameAtlasFactory.BuiltInItemsFolder, overrides);

            GameAssets.AtlasBlocks = GameAtlasFactory.CreateBlocksAtlas(assets);
            GameAssets.AtlasItems = GameAtlasFactory.CreateItemsAtlas(assets);

            GameSetLoader.Load(stack, assets);

            GameAssets.IsInitialized = true;
        }

        public static void Preload(SpaceSceneArgs param, BaseSpaceScene scene, Astronaut astronaut)
        {
            Preload(param);
        }

        public static void Preload(SpaceSceneArgs param)
        {
            bool isMultiplayer = !param.SearchForGameSetInLocalFolder;
            var modFolderName = string.IsNullOrEmpty(param.modfolderName) ? GameSetLocator.FindFolder(param.modId) : param.modfolderName;
            string serverName = isMultiplayer ? param.gameSetServerFolder ?? "" : "";
            var modFolders = ModPath.GetModFolders(param.SearchForGameSetInLocalFolder, serverName);

            var result = GameSetStackResolver.Resolve(modFolders, param.modId, param.extensions, modFolderName);
            foreach (var error in result.Errors) Debug.Error($"[GameSetLoader] {error}");

            if (GameAssets.IsInitialized) GameAssets.DisposeAll();
            Spacebox.Game.Resource.TexturePacks.TexturePackService.SetSessionPacks(param.applyTexturePacks ? param.serverTexturePacks : null);
            InitializeGamesetData(result.Stack, param.applyTexturePacks);
        }
    }
}
