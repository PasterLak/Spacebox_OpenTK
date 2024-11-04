using System.Numerics;

namespace Spacebox.Common.Commands
{
    public class ResourcesCommand : ICommand
    {
        public string Name => "resources";
        public string Description => "";

        public void Execute(string[] args)
        {
          
            GameConsole.AddMessage($"[Shaders cached: {ShaderManager.Count}]", new Vector4(1f, 1f, 0f, 1f));
            GameConsole.AddMessage($"[Textures cached: {TextureManager.Count}]", new Vector4(1f, 1f, 0f, 1f));
        }
    }
}
