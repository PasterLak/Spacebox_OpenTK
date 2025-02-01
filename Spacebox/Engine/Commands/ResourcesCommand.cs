using System.Numerics;

namespace Spacebox.Engine.Commands
{
    public class ResourcesCommand : ICommand
    {
        public string Name => "resources";
        public string Description => "";

        public void Execute(string[] args)
        {
          
            Debug.AddMessage($"[Shaders cached: {ShaderManager.Count}]", new Vector4(1f, 1f, 0f, 1f));
            Debug.AddMessage($"[Textures cached: {TextureManager.Count}]", new Vector4(1f, 1f, 0f, 1f));
        }
    }
}
