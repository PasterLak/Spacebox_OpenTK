using OpenTK.Mathematics;

namespace Engine.Graphics.Atlas;

public sealed class TextureAtlas : IDisposable
{
    private sealed class Entry
    {
        public readonly AtlasRegion Region;
        public Color4[,] Pixels;
        public int Order;

        public Entry(string name, Color4[,] pixels, int order)
        {
            Region = new AtlasRegion(name);
            Pixels = pixels;
            Order = order;
        }

        public int Width => Pixels.GetLength(0);
        public int Height => Pixels.GetLength(1);
    }

    private static readonly Color4 Transparent = new Color4(0, 0, 0, 0);

    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, Color4[,]>> _layers = new Dictionary<string, Dictionary<string, Color4[,]>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D> _layerTextures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private readonly List<Entry> _sortedEntries = new List<Entry>();

    private Texture2D _texture;
    private bool _isDirty = true;
    private int _nextOrder;
    private Entry _fallback;

    private Entry _lastSampled;

    public string Name { get; }
    public int Padding { get; }
    public int MaxSize { get; }
    public FilterMode FilterMode { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Version { get; private set; }
    public int Count => _entries.Count;
    public IEnumerable<string> Names => _entries.Keys;

    public TextureAtlas(string name, int padding = 0, FilterMode filterMode = FilterMode.Nearest, int maxSize = 8192)
    {
        Name = name;
        Padding = Math.Max(0, padding);
        FilterMode = filterMode;
        MaxSize = maxSize;
    }

    public Texture2D Texture
    {
        get
        {
            EnsureBuilt();
            return _texture;
        }
    }

    public Texture2D GetLayerTexture(string layer)
    {
        EnsureBuilt();
        return _layerTextures.TryGetValue(layer, out var texture) ? texture : null;
    }

    public bool Add(string name, Color4[,] pixelsTopDown, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(name) || pixelsTopDown == null) return false;
        if (pixelsTopDown.GetLength(0) == 0 || pixelsTopDown.GetLength(1) == 0) return false;

        if (_entries.TryGetValue(name, out var existing))
        {
            if (!overwrite) return false;
            existing.Pixels = pixelsTopDown;
        }
        else
        {
            var entry = new Entry(name, pixelsTopDown, _nextOrder++);
            _entries.Add(name, entry);
            _fallback ??= entry;
        }

        _isDirty = true;
        return true;
    }

    public bool Add(string name, Texture2D texture, bool overwrite = false)
    {
        if (texture?.GetPixels() == null) return false;
        return Add(name, ImagePixels.FlipY(texture.GetPixels()), overwrite);
    }

    public bool AddFile(string name, string path, bool overwrite = false)
    {
        if (!TryLoadPixels(path, out var pixels)) return false;
        return Add(name, pixels, overwrite);
    }

    public bool AddFile(string path, bool overwrite = false)
    {
        return AddFile(Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), path, overwrite);
    }

    public int AddDirectory(string path, bool recursive = true, bool skipIgnoredFolders = false)
    {
        int added = 0;
        foreach (var file in EnumerateImages(path, recursive, skipIgnoredFolders))
        {
            if (AddFile(file)) added++;
        }
        return added;
    }

    public void CreateLayer(string layer)
    {
        if (_layers.ContainsKey(layer)) return;

        _layers.Add(layer, new Dictionary<string, Color4[,]>(StringComparer.OrdinalIgnoreCase));
        _isDirty = true;
    }

    public bool AddLayer(string layer, string name, Color4[,] pixelsTopDown)
    {
        if (pixelsTopDown == null) return false;

        CreateLayer(layer);
        _layers[layer][name] = pixelsTopDown;
        _isDirty = true;
        return true;
    }

    public int AddLayerDirectory(string layer, string path, bool recursive = true, bool skipIgnoredFolders = false)
    {
        CreateLayer(layer);
        int added = 0;
        foreach (var file in EnumerateImages(path, recursive, skipIgnoredFolders))
        {
            if (TryLoadPixels(file, out var pixels) &&
                AddLayer(layer, Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), pixels))
            {
                added++;
            }
        }
        return added;
    }

    private static bool TryLoadPixels(string path, out Color4[,] pixels)
    {
        if (!Application.IsHeadless) return ImagePixels.TryLoadTopDown(path, out pixels);

        pixels = new Color4[1, 1];
        pixels[0, 0] = Color4.White;
        return File.Exists(path);
    }

    public bool Remove(string name)
    {
        if (!_entries.Remove(name)) return false;

        foreach (var layer in _layers.Values)
            layer.Remove(name);

        if (_fallback != null && string.Equals(_fallback.Region.Name, name, StringComparison.OrdinalIgnoreCase))
            _fallback = null;

        _isDirty = true;
        return true;
    }

    public bool Contains(string name)
    {
        return name != null && _entries.ContainsKey(name);
    }

    public bool TryGetRegion(string name, out AtlasRegion region)
    {
        region = null;
        if (name == null || !_entries.TryGetValue(name, out var entry)) return false;

        EnsureBuilt();
        region = entry.Region;
        return true;
    }

    public AtlasSprite GetSprite(string name)
    {
        return name != null && _entries.TryGetValue(name, out var entry) ? new AtlasSprite(this, entry.Region) : AtlasSprite.None;
    }

    public AtlasRegion GetRegion(string name)
    {
        if (TryGetRegion(name, out var region)) return region;

        Debug.Error($"[TextureAtlas:{Name}] Region not found: {name}");
        EnsureBuilt();
        return _fallback?.Region;
    }

    public bool TryGetPixels(string name, bool bottomUp, out Color4[,] pixels)
    {
        pixels = null;
        if (name == null || !_entries.TryGetValue(name, out var entry)) return false;

        pixels = bottomUp ? ImagePixels.FlipY(entry.Pixels) : ImagePixels.Copy(entry.Pixels);
        return true;
    }

    public bool TrySample(Vector2 uv, Vector2 anchor, out Color4 color)
    {
        color = default;
        EnsureBuilt();
        if (_lastSampled == null || !Contains(_lastSampled.Region, anchor))
        {
            _lastSampled = null;
            foreach (var entry in _entries.Values)
            {
                if (!Contains(entry.Region, anchor)) continue;
                _lastSampled = entry;
                break;
            }
            if (_lastSampled == null) return false;
        }

        var region = _lastSampled.Region;
        float u = (uv.X - region.UvMin.X) / (region.UvMax.X - region.UvMin.X);
        float v = (uv.Y - region.UvMin.Y) / (region.UvMax.Y - region.UvMin.Y);
        int x = Math.Clamp((int)(u * region.Width), 0, region.Width - 1);
        int y = Math.Clamp((int)(v * region.Height), 0, region.Height - 1);
        color = _lastSampled.Pixels[x, region.Height - 1 - y];
        return true;
    }

    private static bool Contains(AtlasRegion region, Vector2 uv)
    {
        return uv.X >= region.UvMin.X && uv.X <= region.UvMax.X && uv.Y >= region.UvMin.Y && uv.Y <= region.UvMax.Y;
    }

    public bool TryGetLayerPixels(string layer, string name, bool bottomUp, out Color4[,] pixels)
    {
        pixels = null;
        if (!_layers.TryGetValue(layer, out var layerPixels) || !layerPixels.TryGetValue(name, out var source)) return false;

        pixels = bottomUp ? ImagePixels.FlipY(source) : ImagePixels.Copy(source);
        return true;
    }

    public Texture2D CreateTexture(string name, bool bottomUp = true)
    {
        return TryGetPixels(name, bottomUp, out var pixels) ? CreateTexture(pixels) : null;
    }

    public Texture2D CreateLayerTexture(string layer, string name, bool bottomUp = true)
    {
        return TryGetLayerPixels(layer, name, bottomUp, out var pixels) ? CreateTexture(pixels) : null;
    }

    public void EnsureBuilt()
    {
        if (_isDirty) Build();
    }

    public void Build()
    {
        _isDirty = false;
        SortEntries();
        Pack();

        var basePixels = CreateBuffer();
        foreach (var entry in _sortedEntries)
        {
            Blit(basePixels, entry.Pixels, entry.Region);
        }
        _texture = Upload(_texture, basePixels, Name);

        foreach (var (layer, layerPixels) in _layers)
        {
            var buffer = CreateBuffer();
            foreach (var (name, pixels) in layerPixels)
            {
                if (_entries.TryGetValue(name, out var entry) && pixels.GetLength(0) == entry.Width && pixels.GetLength(1) == entry.Height)
                    Blit(buffer, pixels, entry.Region);
            }

            _layerTextures.TryGetValue(layer, out var layerTexture);
            _layerTextures[layer] = Upload(layerTexture, buffer, $"{Name}_{layer}");
        }

        Version++;
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;

        foreach (var texture in _layerTextures.Values)
            texture.Dispose();

        _layerTextures.Clear();
        _layers.Clear();
        _entries.Clear();
        _sortedEntries.Clear();
        _fallback = null;
    }

    private void SortEntries()
    {
        _sortedEntries.Clear();
        _sortedEntries.AddRange(_entries.Values);
        _sortedEntries.Sort((a, b) =>
        {
            int byHeight = b.Height.CompareTo(a.Height);
            if (byHeight != 0) return byHeight;
            int byWidth = b.Width.CompareTo(a.Width);
            return byWidth != 0 ? byWidth : a.Order.CompareTo(b.Order);
        });
    }

    private void Pack()
    {
        var sizes = new AtlasRect[_sortedEntries.Count];
        for (int i = 0; i < sizes.Length; i++)
        {
            sizes[i] = new AtlasRect(0, 0, _sortedEntries[i].Width, _sortedEntries[i].Height);
        }

        var placements = new AtlasRect[sizes.Length];
        if (!AtlasLayout.Pack(sizes, Padding, MaxSize, out int width, out int height, placements))
            Debug.Error($"[TextureAtlas:{Name}] Textures do not fit into {MaxSize}x{MaxSize}");

        Width = width;
        Height = height;

        for (int i = 0; i < placements.Length; i++)
        {
            var rect = placements[i];
            _sortedEntries[i].Region.Set(rect.X, rect.Y, rect.Width, rect.Height, width, height);
        }
    }

    private Color4[,] CreateBuffer()
    {
        var buffer = new Color4[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                buffer[x, y] = Transparent;
        return buffer;
    }

    private void Blit(Color4[,] target, Color4[,] sourceTopDown, AtlasRegion region)
    {
        int width = sourceTopDown.GetLength(0);
        int height = sourceTopDown.GetLength(1);

        for (int y = -Padding; y < height + Padding; y++)
        {
            int sourceY = Math.Clamp(y, 0, height - 1);
            int targetY = region.Y + (height - 1 - y);
            if (targetY < 0 || targetY >= Height) continue;

            for (int x = -Padding; x < width + Padding; x++)
            {
                int targetX = region.X + x;
                if (targetX < 0 || targetX >= Width) continue;

                target[targetX, targetY] = sourceTopDown[Math.Clamp(x, 0, width - 1), sourceY];
            }
        }
    }

    private Texture2D Upload(Texture2D texture, Color4[,] pixels, string label)
    {
        if (texture == null || texture.IsDisposed || texture.Width != Width || texture.Height != Height)
        {
            texture?.Dispose();
            texture = new Texture2D(Width, Height, FilterMode == FilterMode.Nearest);
            Resources.RemoveResourceFromControll(texture);
            GPUDebug.LabelTexture(texture.Handle, label);
        }

        texture.SetPixelsData(pixels);
        texture.UpdateTexture();
        texture.FilterMode = FilterMode;
        return texture;
    }

    private static Texture2D CreateTexture(Color4[,] pixels)
    {
        var texture = new Texture2D(pixels.GetLength(0), pixels.GetLength(1), true);
        texture.SetPixelsData(pixels);
        texture.UpdateTexture();
        texture.FilterMode = FilterMode.Nearest;
        return texture;
    }

    public static bool IsInIgnoredFolder(string root, string file)
    {
        var relativeFolder = Path.GetDirectoryName(Path.GetRelativePath(root, file));
        if (string.IsNullOrEmpty(relativeFolder)) return false;

        foreach (var segment in relativeFolder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.StartsWith('_')) return true;
        }
        return false;
    }

    private static IEnumerable<string> EnumerateImages(string path, bool recursive, bool skipIgnoredFolders)
    {
        if (!Directory.Exists(path))
        {
            Debug.Error($"[TextureAtlas] Directory was not found: {path}");
            yield break;
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, "*.*", option);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (skipIgnoredFolders && IsInIgnoredFolder(path, file)) continue;

            var extension = Path.GetExtension(file);
            for (int i = 0; i < ImagePixels.SupportedExtensions.Length; i++)
            {
                if (string.Equals(extension, ImagePixels.SupportedExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                    break;
                }
            }
        }
    }

}
