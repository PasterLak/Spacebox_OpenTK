

namespace Engine.Utils
{
    public class ShaderHotReloader : IDisposable
    {
        private FileSystemWatcher _watcher;
        public ShaderHotReloader(string shaderPath, FileSystemEventHandler onChanged)
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(shaderPath), Path.GetFileName(shaderPath) + ".glsl")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _watcher.Changed += onChanged;
        }
        public static ShaderHotReloader TryCreate(string shaderPath, FileSystemEventHandler onChanged)
        {
            try
            {
                return new ShaderHotReloader(shaderPath, onChanged);
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is PlatformNotSupportedException)
            {
                Debug.Warning($"[Shader] Hot reload is off for {shaderPath}: {ex.Message}");
                return null;
            }
        }

        public void Disable()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;
        }
        public void Enable()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = true;
        }
        public void Dispose() => _watcher?.Dispose();
    }
}
