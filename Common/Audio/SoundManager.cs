
namespace Spacebox.Common.Audio
{
    public static class SoundManager
    {
        private class AudioClipEntry
        {
            public AudioClip Clip { get; }
            public bool IsPermanent { get; }

            public AudioClipEntry(AudioClip clip, bool isPermanent)
            {
                Clip = clip;
                IsPermanent = isPermanent;
            }
        }

        private static readonly Dictionary<string, AudioClipEntry> audioClips = new Dictionary<string, AudioClipEntry>();
        private static readonly object cacheLock = new object();
        private const string DefaultAudioPath = "Resources/Audio/error.wav";
        public static readonly List<string> AllowedExtensions = new List<string> { ".wav", ".ogg" };


        public static AudioClip AddClip(string name, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            return GetClip(name, loadMode);
        }
        public static AudioClip GetClip(string name, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out var existingEntry))
                {
                    return existingEntry.Clip;
                }

                try
                {
                    string filePath = AudioPathResolver.ResolvePath(name, AppDomain.CurrentDomain.BaseDirectory, AllowedExtensions);
                    if (filePath == null)
                    {
                        throw new FileNotFoundException($"Audio file for '{name}' not found.");
                    }

                    var clip = new AudioClip(filePath);
                    var entry = new AudioClipEntry(clip, isPermanent: false);
                    audioClips[name] = entry;
                    return clip;
                }
                catch (Exception ex)
                {
                    Debug.Error($"[SoundManager] Failed to load audio clip '{name}': {ex.Message}");
                    if (name != DefaultAudioPath)
                    {
                        try
                        {
                            var defaultClip = GetClip(DefaultAudioPath, loadMode);
                            Debug.Log($"[SoundManager] Loaded default audio clip '{DefaultAudioPath}' instead.");
                            return defaultClip;
                        }
                        catch (Exception defaultEx)
                        {
                            Debug.Error($"[SoundManager] Failed to load default audio clip '{DefaultAudioPath}': {defaultEx.Message}");
                            throw new Exception($"Failed to load audio clip '{name}' and default audio clip '{DefaultAudioPath}'.", ex);
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to load default audio clip '{DefaultAudioPath}'.", ex);
                    }
                }
            }
        }

        public static AudioClip AddPermanentClip(string name, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out var existingEntry))
                {
                    if (!existingEntry.IsPermanent)
                    {
                        var newEntry = new AudioClipEntry(existingEntry.Clip, isPermanent: true);
                        audioClips[name] = newEntry;
                    }
                    return existingEntry.Clip;
                }

                try
                {
                    string filePath = AudioPathResolver.ResolvePath(name, AppDomain.CurrentDomain.BaseDirectory, AllowedExtensions);
                    if (filePath == null)
                    {
                        throw new FileNotFoundException($"Audio file for '{name}' not found.");
                    }

                    var clip = new AudioClip(filePath);
                    var entry = new AudioClipEntry(clip, isPermanent: true);
                    audioClips[name] = entry;
                    return clip;
                }
                catch (Exception ex)
                {
                    Debug.Error($"[SoundManager] Failed to load permanent audio clip '{name}': {ex.Message}");
                    if (name != DefaultAudioPath)
                    {
                        try
                        {
                            var defaultClip = AddPermanentClip(DefaultAudioPath, loadMode);
                            Debug.Log($"[SoundManager] Loaded default audio clip '{DefaultAudioPath}' as permanent instead.");
                            return defaultClip;
                        }
                        catch (Exception defaultEx)
                        {
                            Debug.Error($"[SoundManager] Failed to load default audio clip '{DefaultAudioPath}': {defaultEx.Message}");
                            throw new Exception($"Failed to load permanent audio clip '{name}' and default audio clip '{DefaultAudioPath}'.", ex);
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to load default audio clip '{DefaultAudioPath}'.", ex);
                    }
                }
            }
        }

        public static void RemoveClip(string name)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out var entry))
                {
                    entry.Clip.Dispose();
                    audioClips.Remove(name);
                }
            }
        }

        public static void Dispose()
        {
            lock (cacheLock)
            {
                var unloadableClips = audioClips.Where(kv => !kv.Value.IsPermanent).ToList();
                foreach (var kv in unloadableClips)
                {
                    kv.Value.Clip.Dispose();
                    audioClips.Remove(kv.Key);
                }

                Debug.Log($"[SoundManager] {unloadableClips.Count} audio clips have been disposed.");
            }
        }

        public static void DisposeAll()
        {
            lock (cacheLock)
            {
                var count = audioClips.Count;
                foreach (var kv in audioClips.ToList())
                {
                    kv.Value.Clip.Dispose();
                }
                audioClips.Clear();
                Debug.Log($"[SoundManager] {count} audio clips have been disposed.");
            }
        }
    }
}
