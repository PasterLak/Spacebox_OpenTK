using OpenTK.Mathematics;

namespace Engine
{
    public static class TextureManager
    {
        private class TextureEntry
        {
            public Texture2D Texture { get; }
            public bool IsPermanent { get; }

            public TextureEntry(Texture2D texture, bool isPermanent)
            {
                Texture = texture;
                IsPermanent = isPermanent;
            }
        }

        private static readonly Dictionary<string, TextureEntry> Textures = new Dictionary<string, TextureEntry>();
        private static readonly object _lock = new object();
        private static readonly Texture2D DefaultTexture;

        public static int Count => Textures.Count;

        static TextureManager()
        {
            DefaultTexture = CreateDefaultPinkTexture();
        }        

        private static Texture2D CreateDefaultPinkTexture()
        {
            var texture = new Texture2D(1, 1, pixelated: true);
            texture.SetPixel(0, 0, new Color4(1f, 0f, 1f, 1f)); // Pink
            texture.UpdateTexture();
            texture.Use();
            return texture;
        }

      

        public static Texture2D GetTexture(string texturePath)
        {
            return GetTexture(texturePath, pixelated: false, true);
        }

        public static Texture2D GetTexture(string texturePath, bool pixelated)
        {
            return GetTexture(texturePath, pixelated, true);
        }

        public static Texture2D GetTexture(string texturePath, bool pixelated, bool mirrowY)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                throw new ArgumentException("Texture path cannot be null or empty.", nameof(texturePath));

            lock (_lock)
            {
                if (Textures.TryGetValue(texturePath, out var existingEntry))
                {
                    return existingEntry.Texture;
                }

                if (!File.Exists(texturePath))
                {
                    Debug.Error($"[TextureManager] Texture file '{texturePath}' does not exist.");
                    return DefaultTexture;
                }

                try
                {
                    var texture = new Texture2D(texturePath, pixelated, mirrowY);
                    var entry = new TextureEntry(texture, isPermanent: false);
                    Textures.Add(texturePath, entry);
                    return texture;
                }
                catch (Exception ex)
                {
                    Debug.Error($"[TextureManager] Failed to load texture '{texturePath}': {ex.Message}");
                    return DefaultTexture;
                }
            }
        }

        public static Texture2D AddPermanentTexture(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                throw new ArgumentException("Texture path cannot be null or empty.", nameof(texturePath));

            lock (_lock)
            {
                if (Textures.TryGetValue(texturePath, out var existingEntry))
                {
                    if (!existingEntry.IsPermanent)
                    {
                        var newEntry = new TextureEntry(existingEntry.Texture, isPermanent: true);
                        Textures[texturePath] = newEntry;
                        Debug.Log($"[TextureManager] Texture '{texturePath}' marked as permanent.");
                    }
                    return existingEntry.Texture;
                }

                if (!File.Exists(texturePath))
                {
                    Debug.Error($"[TextureManager] Texture file '{texturePath}' does not exist.");
                    return DefaultTexture;
                }

                try
                {
                    var texture = new Texture2D(texturePath, pixelated: false);
                    var entry = new TextureEntry(texture, isPermanent: true);
                    Textures.Add(texturePath, entry);
                    return texture;
                }
                catch (Exception ex)
                {
                    Debug.Error($"[TextureManager] Failed to load permanent texture '{texturePath}': {ex.Message}");
                    return DefaultTexture;
                }
            }
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                var unloadableTextures = Textures.Where(kv => !kv.Value.IsPermanent).ToList();
                foreach (var kv in unloadableTextures)
                {
                    kv.Value.Texture.Dispose();
                    Textures.Remove(kv.Key);
                }

                Debug.Log($"[TextureManager] {unloadableTextures.Count} texture(s) have been disposed.");
            }
        }

        public static void DisposeAll()
        {
            lock (_lock)
            {
                var allTextures = Textures.ToList();
                foreach (var kv in allTextures)
                {
                    kv.Value.Texture.Dispose();
                }
                Textures.Clear();
                Debug.Log($"[TextureManager] All {allTextures.Count} textures have been disposed.");
            }
        }
    }
}
