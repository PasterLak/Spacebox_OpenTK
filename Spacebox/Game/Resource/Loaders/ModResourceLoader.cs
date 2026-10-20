using System.IO;
using Engine;

namespace Spacebox.Game.Resource
{
    public static class ModResourceLoader
    {
        public static T Load<T>(string modFolderPath, string relativePath) where T : IResource, new()
        {
            string modResourcesDir = Path.Combine(modFolderPath, "Resources");

            if (!Directory.Exists(modResourcesDir))
            {
                return Engine.Resources.Load<T>(relativePath);
            }

            string modFilePath = Path.Combine(modFolderPath, relativePath);

            if (File.Exists(modFilePath))
            {
                return Engine.Resources.Load<T>(modFilePath);
            }

            return Engine.Resources.Load<T>(relativePath);
        }
    }
}