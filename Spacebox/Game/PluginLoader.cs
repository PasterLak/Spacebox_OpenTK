using System.Runtime.Loader;
using Engine;
using Spacebox.Core;

namespace Spacebox.Game
{
    public class PluginLoader
    {
       
        public List<IPlugin> LoadedMods { get; private set; } = new List<IPlugin>();

        private IGameApi _gameApi = new Spacebox.Game.Plugins.GameApiImplementation();

        public void LoadMods(string pluginsPath)
        {
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
                return;
            }

            string[] modDirectories = Directory.GetDirectories(pluginsPath);

            if(modDirectories.Length == 0)
            {
                Debug.Warning("[PluginLoader] No mods found in Plugins directory.");
                return;
            }

            foreach (var modDir in modDirectories)
            {

                string[] dllFiles = Directory.GetFiles(modDir, "*.dll");

                foreach (var dllPath in dllFiles)
                {
                    TryLoadModAssembly(dllPath);
                }
            }
        }

        private void TryLoadModAssembly(string path)
        {
            try
            {

                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));

                var modTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in modTypes)
                {
                    IPlugin modInstance = (IPlugin)Activator.CreateInstance(type);

                    if (modInstance != null)
                    {
                        LoadedMods.Add(modInstance);

                        modInstance.OnInitialize(_gameApi);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[PluginLoader] Failed to load mod from {path}: {ex.Message}");
            }
        }
    }
}