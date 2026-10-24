
using Engine.Audio;
using Engine.SceneManagement;
using Engine.Utils;
using OpenTK.Mathematics;
using static Engine.Utils.Testing;

namespace Engine
{
    public interface IResource : IDisposable
    {
        bool IsDisposed { get; set; }

        IResource Load(string path);
        string GetResourceInfo();
        int GetHandle();

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

        private static List<IResource> _loadedResources = new List<IResource>();
        private static readonly object _ownershipSync = new object();
        private static readonly Dictionary<IResource, int> _cacheOwners = new Dictionary<IResource, int>(ReferenceEqualityComparer.Instance);
        private static readonly HashSet<(Type, string)> _failedLoads = new HashSet<(Type, string)>();

        public static T Load<T>(string pathOrName, bool global = false) where T : IResource, new()
        {
            string canonicalKey = NormalizeKey(pathOrName);
            string shortKey = GetShortKey(canonicalKey);

            if (_resourcesByType.TryGetValue(typeof(T), out var dict))
            {
                if (dict.TryGetValue(canonicalKey, out var entry))
                    return (T)Promote(dict, canonicalKey, entry, global);
                if (canonicalKey == shortKey && _aliasMap.TryGetValue(typeof(T), out var aliasDict) && aliasDict.TryGetValue(shortKey, out var actualKey))
                {
                    if (dict.TryGetValue(actualKey, out var existingEntry))
                        return (T)Promote(dict, actualKey, existingEntry, global);
                }
            }

            if (_failedLoads.Contains((typeof(T), canonicalKey))) return ResourceFallbacks.Get<T>();

            T resource = new T();
            try
            {
                resource = (T)resource.Load(pathOrName);

                if (resource == null)
                {
                    _failedLoads.Add((typeof(T), canonicalKey));
                    Debug.Error($"[Resources][Load] Resource was null {typeof(T).Name} {pathOrName}");
                    return ResourceFallbacks.Get<T>();
                }
            }
            catch (Exception ex)
            {
                // resource = (T)resource.Default();
                // return resource;
                // Debug.Log($"Resource error {typeof(T)} {pathOrName}");
                _failedLoads.Add((typeof(T), canonicalKey));
                Debug.Error($"[Resources][Load] Error loading {typeof(T).Name} from {pathOrName}. Default one will be used. Error: {ex}");
                return ResourceFallbacks.Get<T>();
                // resource = (T)Get<T>(pathOrName);
            }
            AddResource<T>(pathOrName, resource, global);
            return resource;
        }

        private static IResource Promote(Dictionary<string, ResourceEntry> dict, string key, ResourceEntry entry, bool global)
        {
            if (global && !entry.Global)
            {
                entry.Global = true;
                dict[key] = entry;
            }
            return entry.Resource;
        }

        public static void LoadAll<T>(string[] paths) where T : IResource, new()
        {
            foreach (var path in paths)
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
           // Debug.Warning($"[Resources][Get] Resource not found: {typeof(T).Name} {pathOrName}. Loading it now.");
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
                    dict.Remove(canonicalKey);
                    ReleaseEntry(entry.Resource);
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
                if (!IsCacheOwned(resource)) ResourceScopes.Own(resource);
                _loadedResources.Add(resource);
            }
        }

        public static bool IsCacheOwned(IResource resource)
        {
            if (resource == null) return false;
            lock (_ownershipSync) return _cacheOwners.ContainsKey(resource);
        }

        public static bool BlocksDispose(IResource resource)
        {
            if (!IsCacheOwned(resource)) return false;
            Debug.Warning($"[Resources] Ignored Dispose of a cached resource, the cache releases it: {resource.GetResourceInfo()}");
            return true;
        }

        public static bool Release(IResource resource)
        {
            if (resource == null || resource.IsDisposed || IsCacheOwned(resource)) return false;
            resource.Dispose();
            return true;
        }

        private static void TakeOwnership(IResource resource)
        {
            ResourceScopes.Own(resource);
            lock (_ownershipSync)
            {
                _cacheOwners.TryGetValue(resource, out int count);
                _cacheOwners[resource] = count + 1;
            }
        }

        private static void ReleaseEntry(IResource resource)
        {
            if (resource == null) return;
            lock (_ownershipSync)
            {
                if (!_cacheOwners.TryGetValue(resource, out int count)) return;
                if (count > 1)
                {
                    _cacheOwners[resource] = count - 1;
                    return;
                }
                _cacheOwners.Remove(resource);
            }
            resource.Dispose();
        }


        private static void DisposeAllResources()
        {


            for (int i = _loadedResources.Count - 1; i >= 0; i--)
            {
                var res = _loadedResources[i];

                if (res != null)
                {
                    Release(res);
                    _loadedResources[i] = null;
                }

            }
            _loadedResources.Clear();


        }


        public static void UnloadAll(bool clearGlobal = false)
        {
            _failedLoads.Clear();
            foreach (var kvp in _resourcesByType)
            {
                var dict = kvp.Value;
                var keys = new List<string>(dict.Keys);
                foreach (var key in keys)
                {
                    var entry = dict[key];
                    if (!entry.Global || clearGlobal)
                    {
                        dict.Remove(key);
                        ReleaseEntry(entry.Resource);
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

            Debug.Log(">------------------------------------<", Color4.Yellow);
            foreach (KeyValuePair<Type, Dictionary<string, ResourceEntry>> kvp in _resourcesByType)
            {
                var type = kvp.Key;
                var dict = kvp.Value;

                if (dict.Count == 0) continue;


                Debug.Log($"- [{type.Name}] count: {dict.Count}", Color4.Yellow);
                foreach (string key in dict.Keys)
                {
                    var text = dict[key].Global ? "[G]" : "   ";

                    text += $"[{dict[key].Resource.GetHandle()}]";

                    Debug.Log($"  > {text}{key}");
                }

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
            bool replaced = dict.TryGetValue(canonicalKey, out var previous);
            if (!replaced || !ReferenceEquals(previous.Resource, resource))
            {
                TakeOwnership(resource);
                if (replaced) ReleaseEntry(previous.Resource);
            }
            dict[canonicalKey] = new ResourceEntry { Resource = resource, Global = global || (replaced && previous.Global && ReferenceEquals(previous.Resource, resource)) };
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
