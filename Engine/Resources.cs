
using Engine.Audio;
using Engine.SceneManagement;
using Engine.Utils;
using OpenTK.Mathematics;

namespace Engine
{
    public interface IResource : IDisposable
    {
        IResource Load(string path);

    }
    public static class Resources
    {
        private struct ResourceEntry
        {
            public IResource Resource;
            public bool Global;
        }

        private static readonly Dictionary<Type, Dictionary<string, ResourceEntry>> _resourcesByType = new Dictionary<Type, Dictionary<string, ResourceEntry>>();
        private static readonly Dictionary<Type, Dictionary<string, string>> _aliasMap = new Dictionary<Type, Dictionary<string, string>>();

        private static readonly Dictionary<Type, IResource> _error = new Dictionary<Type, IResource>();

        private static List<IResource> _loadedResources = new List<IResource>();
        static Resources()
        {
            LoadError();
        }

        private static void LoadError()
        {
            _error.Add(typeof(Texture2D), Texture2D.CreateTexture(1, 1, Color4.Pink, FilterMode.Nearest));
            _error.Add(typeof(Shader), new Shader("Shaders/colored"));
            _error.Add(typeof(AudioClip), new AudioClip("Resources/Audio/error.ogg"));
            _error.Add(typeof(Mesh), new Mesh("Resources/Models/cube.obj"));
        }
        public static T Load<T>(string pathOrName, bool global = false) where T : IResource, new()
        {
            string canonicalKey = NormalizeKey(pathOrName);
            string shortKey = GetShortKey(canonicalKey);

            if (_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                if (dict.TryGetValue(canonicalKey, out var entry))
                    return (T)entry.Resource;
                if (_aliasMap.TryGetValue(typeof(T), out var aliasDict) && aliasDict.TryGetValue(shortKey, out var actualKey))
                {
                    if (dict.TryGetValue(actualKey, out var existingEntry))
                        return (T)existingEntry.Resource;
                }
            }

            T resource = new T();
            try
            {
                resource = (T)resource.Load(pathOrName);

                if (resource == null)
                {
                    Debug.Error($"[Resources][Load] Resource was null {typeof(T).Name} {pathOrName}");
                    return (T)_error[typeof(T)];
                }
            }
            catch(Exception ex)
            {
                // resource = (T)resource.Default();
                // return resource;
                // Debug.Log($"Resource error {typeof(T)} {pathOrName}");
                Debug.Error($"[Resources][Load] Error loading {typeof(T).Name} from {pathOrName}. Default one will be used. Error: {ex}");
                return (T)_error[typeof(T)];
                // resource = (T)Get<T>(pathOrName);
            }
            AddResource<T>(pathOrName, resource, global);
            return resource;
        }

        public static void LoadAll<T>(string[] paths) where T : IResource, new()
        {
            foreach(var path in paths)
            {
                Load<T>(path);
            }
        }

        public static void LoadAllAsync<T>(string[] paths) where T : IResource, new()
        {
            var type = typeof(T);
            if (type == typeof(Texture2D))
            {
                LoadAllTextures(paths);
            }
            if (type == typeof(Shader))
            {
                foreach (var path in paths)
                {
                    Load<T>(path);
                }
            }
            if (type == typeof(AudioClip))
            {
                foreach (var path in paths)
                {
                    Load<T>(path);
                }
            }
            if (type == typeof(Mesh))
            {
                LoadAllMeshes(paths);
            }

        }

        public static List<AudioClip> LoadAllAudioClips(string[] paths)
        {
            var tasks = paths.Select(path => Task.Run(() =>
            {
                try
                {
                    AudioClip clip = new AudioClip(path);
                    AddResource<AudioClip>(path, clip, false);
                    return clip;
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error loading AudioClip '{path}': {ex.Message}");
                    return null;
                }
            })).ToArray();
            Task.WaitAll(tasks);
            List<AudioClip> clips = tasks.Where(t => t.Result != null).Select(t => t.Result).ToList();
            Debug.Log("Loaded all audio clips.", Color4.Yellow);
            return clips;
        }
        private static List<Mesh> LoadAllMeshes(string[] paths, bool global = false)
        {
            var tasks = paths.Select(path => Task.Run(() => ObjLoader.Load(path))).ToArray();
            try { Task.WaitAll(tasks); } catch { }
            List<Mesh> meshes = new List<Mesh>();
            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    var data = tasks[i].Result;
                    Mesh mesh = new Mesh(data.vertices, data.indices);
                    AddResource<Mesh>(paths[i], mesh, global);
                    meshes.Add(mesh);
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error loading mesh '{paths[i]}': {ex.Message}");
                }
            }
          
            return meshes;
        }
        private static List<Texture2D> LoadAllTextures(string[] paths, FilterMode filterMode = FilterMode.Linear, bool flipY = true)
        {
            var loadTasks = paths.Select(path => PixelDataLoader.LoadAsync(path, flipY)).ToArray();
            try
            {
                Task.WaitAll(loadTasks);
            }
            catch { }

            List<Texture2D> textures = new List<Texture2D>();
            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    PixelData data = loadTasks[i].Result;
                    Texture2D tex = new Texture2D(data, filterMode);
                    AddResource<Texture2D>(paths[i], tex, false);
                    textures.Add(tex);
                }
                catch (Exception ex)
                {
                    Debug.Error($"Error loading texture '{paths[i]}': {ex.Message}");
                }
            }
         
            return textures;
        }


        public static T Get<T>(string pathOrName) where T : class, IResource, new()
        {
            string canonicalKey = NormalizeKey(pathOrName);
            string shortKey = GetShortKey(canonicalKey);
            if (_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                if (dict.TryGetValue(canonicalKey, out var entry))
                    return (T)entry.Resource;
                if (_aliasMap.TryGetValue(typeof(T), out var aliasDict) && aliasDict.TryGetValue(shortKey, out var actualKey))
                {
                    if (dict.TryGetValue(actualKey, out var existingEntry))
                        return (T)existingEntry.Resource;
                }
            }

            return Load<T>(pathOrName);
        }

        public static bool Has<T>(string pathOrName) where T : class, IResource, new()
        {
            string canonicalKey = NormalizeKey(pathOrName);

            if (_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                if (dict.ContainsKey(canonicalKey))
                    return true;
                string shortKey = GetShortKey(canonicalKey);
                if (_aliasMap.TryGetValue(typeof(T), out var aliasDict))
                    return aliasDict.ContainsKey(shortKey);
            }
            return false;
        }


        public static void Unload<T>(string pathOrName) where T : IResource
        {
            string canonicalKey = NormalizeKey(pathOrName);
            string shortKey = GetShortKey(canonicalKey);
            if (_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                if (dict.TryGetValue(canonicalKey, out var entry))
                {
                    entry.Resource.Dispose();
                    dict.Remove(canonicalKey);
                }
                if (_aliasMap.TryGetValue(typeof(T), out var aliasDict))
                {
                    if (aliasDict.ContainsKey(shortKey) && aliasDict[shortKey] == canonicalKey)
                        aliasDict.Remove(shortKey);
                }
            }
        }
        public static void AddResourceToDispose(IResource resource)
        {
            if (resource != null && !_loadedResources.Contains(resource))
            {
                _loadedResources.Add(resource);
            }
        }

    
        private static void DisposeAllResources()
        {
            foreach (var resource in _loadedResources)
            {
                resource?.Dispose();
            }
            _loadedResources.Clear();
        }


        public static void UnloadAll(bool clearGlobal = false)
        {
            foreach (var kvp in _resourcesByType)
            {
                var dict = kvp.Value;
                var keys = new List<string>(dict.Keys);
                foreach (var key in keys)
                {
                    if (!dict[key].Global || clearGlobal)
                    {
                        dict[key].Resource.Dispose();
                        dict.Remove(key);
                    }
                }
                if (_aliasMap.TryGetValue(kvp.Key, out var aliasDict))
                {
                    var aliasKeys = new List<string>(aliasDict.Keys);
                    foreach (var ak in aliasKeys)
                    {
                        if (!dict.ContainsKey(aliasDict[ak]))
                            aliasDict.Remove(ak);
                    }
                }
            }

            DisposeAllResources();
        }

        public static void PrintLoadedResources()
        {

            int sum = 0;

            foreach (var t in _resourcesByType)
            {
                sum += t.Value.Count;
            }
            Debug.Log($"[Resources][Scene: {SceneManager.Current.Name}] Loaded resources: {sum}", Color4.Yellow);

            if (sum == 0) return;

            Debug.Log(">------------------------------------<" , Color4.Yellow);
            foreach (var kvp in _resourcesByType)
            {
                var type = kvp.Key;
                var dict = kvp.Value;

                if (dict.Count == 0) continue;

                Debug.Log($"- [{type.Name}] count: {dict.Count}");
                foreach (var key in dict.Keys)
                    Debug.Log($"  > {key}");
            }
            Debug.Log(">------------------------------------<", Color4.Yellow);

        }

        public static void AddResource<T>(string pathOrName, T resource, bool global) where T : IResource
        {
            string canonicalKey = NormalizeKey(pathOrName);
            string shortKey = GetShortKey(canonicalKey);
            if (!_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                dict = new Dictionary<string, ResourceEntry>();
                _resourcesByType[typeof(T)] = dict;
            }
            dict[canonicalKey] = new ResourceEntry { Resource = resource, Global = global };
            if (!_aliasMap.TryGetValue(typeof(T), out var aliasDict))
            {
                aliasDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _aliasMap[typeof(T)] = aliasDict;
            }
            aliasDict[shortKey] = canonicalKey;
        }

        private static string NormalizeKey(string pathOrName)
        {
            string withoutExt = Path.ChangeExtension(pathOrName, null);
            withoutExt = withoutExt.Replace('\\', '/');
            return withoutExt;
        }

        private static string GetShortKey(string canonicalKey)
        {
            return Path.GetFileName(canonicalKey);
        }
    }
}
