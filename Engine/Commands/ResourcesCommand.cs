namespace Engine.Commands
{
    public class ResourcesCommand : CommandBase
    {
        public override string Name => "resources";
        public override string Description => "List of currently used game assets";

        public override void Execute(string[] args)
        {
          
            //Debug.AddMessage($"[Shaders cached: {ShaderManager.Count}]", new Vector4(1f, 1f, 0f, 1f));
            //Debug.AddMessage($"[Textures cached: {TextureManager.Count}]", new Vector4(1f, 1f, 0f, 1f));

            Resources.PrintLoadedResources();
        }
    }
}
