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
        public static bool ApplyTexturePacks = true;

        private static void InitializeGamesetData(GameSetStack stack)
        {
            var layers = stack?.Layers ?? Array.Empty<ContentLayer>();
            var overrides = ApplyTexturePacks ? Spacebox.Game.Resource.TexturePacks.TexturePackService.ActiveOverrides() : null;
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
            string modsFolder = ModPath.GetModsPath(param.SearchForGameSetInLocalFolder, serverName);

            var result = GameSetStackResolver.Resolve(modsFolder, param.modId, param.extensions, modFolderName);
            foreach (var error in result.Errors) Debug.Error($"[GameSetLoader] {error}");

            if (GameAssets.IsInitialized) GameAssets.DisposeAll();
            InitializeGamesetData(result.Stack);
        }
    }
}
