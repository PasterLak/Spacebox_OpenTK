
namespace Spacebox.Common
{
    public static class ShaderManager
    {
        private class ShaderEntry
        {
            public Shader Shader { get; }
            public bool IsPermanent { get; }

            public ShaderEntry(Shader shader, bool isPermanent)
            {
                Shader = shader;
                IsPermanent = isPermanent;
            }
        }

        private static readonly Dictionary<string, ShaderEntry> Shaders = new Dictionary<string, ShaderEntry>();
        private static readonly object _lock = new object();
        private const string DefaultShaderPath = "Shaders/colored";

        public static int Count => Shaders.Count;

        public static Shader GetShader(string shaderPath)
        {
            if (string.IsNullOrWhiteSpace(shaderPath))
                throw new ArgumentException("Shader path cannot be null or empty.", nameof(shaderPath));

            lock (_lock)
            {
                if (Shaders.TryGetValue(shaderPath, out var existingEntry))
                {
                    return existingEntry.Shader;
                }

                try
                {
                    var shader = new Shader(shaderPath);
                    var entry = new ShaderEntry(shader, isPermanent: false);
                    Shaders.Add(shaderPath, entry);
                    return shader;
                }
                catch (Exception ex)
                {
                    Debug.DebugError($"[ShaderManager] Failed to load shader '{shaderPath}': {ex.Message}");
                    if (shaderPath != DefaultShaderPath)
                    {
                        try
                        {
                            var defaultShader = GetShader(DefaultShaderPath);
                            Debug.Log($"[ShaderManager] Loaded default shader '{DefaultShaderPath}' instead.");
                            return defaultShader;
                        }
                        catch (Exception defaultEx)
                        {
                            Debug.DebugError($"[ShaderManager] Failed to load default shader '{DefaultShaderPath}': {defaultEx.Message}");
                            throw new Exception($"Failed to load shader '{shaderPath}' and default shader '{DefaultShaderPath}'.", ex);
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to load default shader '{DefaultShaderPath}'.", ex);
                    }
                }
            }
        }

        public static Shader AddPermanentShader(string shaderPath)
        {
            if (string.IsNullOrWhiteSpace(shaderPath))
                throw new ArgumentException("Shader path cannot be null or empty.", nameof(shaderPath));

            lock (_lock)
            {
                if (Shaders.TryGetValue(shaderPath, out var existingEntry))
                {
                    if (!existingEntry.IsPermanent)
                    {
                        try
                        {
                            var newEntry = new ShaderEntry(existingEntry.Shader, isPermanent: true);
                            Shaders[shaderPath] = newEntry;
                        }
                        catch (Exception ex)
                        {
                            Debug.DebugError($"[ShaderManager] Failed to mark shader '{shaderPath}' as permanent: {ex.Message}");
                            throw;
                        }
                    }
                    return existingEntry.Shader;
                }

                try
                {
                    var shader = new Shader(shaderPath);
                    var entry = new ShaderEntry(shader, isPermanent: true);
                    Shaders.Add(shaderPath, entry);
                    return shader;
                }
                catch (Exception ex)
                {
                    Debug.DebugError($"[ShaderManager] Failed to load permanent shader '{shaderPath}': {ex.Message}");
                    if (shaderPath != DefaultShaderPath)
                    {
                        try
                        {
                            var defaultShader = AddPermanentShader(DefaultShaderPath);
                            Debug.Log($"[ShaderManager] Loaded default shader '{DefaultShaderPath}' as permanent instead.");
                            return defaultShader;
                        }
                        catch (Exception defaultEx)
                        {
                            Debug.DebugError($"[ShaderManager] Failed to load default shader '{DefaultShaderPath}': {defaultEx.Message}");
                            throw new Exception($"Failed to load permanent shader '{shaderPath}' and default shader '{DefaultShaderPath}'.", ex);
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to load default shader '{DefaultShaderPath}'.", ex);
                    }
                }
            }
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                var unloadableShaders = Shaders.Where(kv => !kv.Value.IsPermanent).ToList();
                foreach (var kv in unloadableShaders)
                {
                    kv.Value.Shader.Dispose();
                    Shaders.Remove(kv.Key);
                }

                Debug.Log($"[ShaderManager] {unloadableShaders.Count} shaders have been disposed.");
            }
        }

        public static void DisposeAll()
        {
            lock (_lock)
            {
                var count = Shaders.Count;
                foreach (var kv in Shaders.ToList())
                {
                    kv.Value.Shader.Dispose();
                }
                Shaders.Clear();
                Debug.Log($"[ShaderManager] {count} shaders have been disposed.");
            }
        }
    }
}
