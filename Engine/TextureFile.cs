using OpenTK.Mathematics;
using System.IO.Compression;
using System.Text;


namespace Engine
{
    public class TextureFile
    {
        public enum SaveMode
        {
            Text,
            Binary,
            Compressed,
            All
        }

        public enum PaletteMode
        {
            None,
            Force,
            Auto
        }

        public static void Save(string filePath, Color4[] pixels, bool hasAlpha, SaveMode mode = SaveMode.Text, PaletteMode paletteMode = PaletteMode.Auto)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            bool usePalette = ShouldUsePalette(pixels, paletteMode);

            if (usePalette)
            {
                var (palette, indices) = CreatePalette(pixels, hasAlpha);
                SaveWithPalette(filePath, palette, indices, hasAlpha, mode);
            }
            else
            {
                SaveWithoutPalette(filePath, pixels, hasAlpha, mode);
            }
        }

        private static bool ShouldUsePalette(Color4[] pixels, PaletteMode paletteMode)
        {
            if (paletteMode == PaletteMode.Force) return true;
            if (paletteMode == PaletteMode.None) return false;

            var uniqueColors = new HashSet<Color4>();
            foreach (var color in pixels)
            {
                uniqueColors.Add(color);
                if (uniqueColors.Count > pixels.Length * (color.A < 1.0f ? 0.5f : 0.33f))
                    return false;
            }
            return true;
        }

        private static (List<Color4> palette, List<int> indices) CreatePalette(Color4[] pixels, bool hasAlpha)
        {
            var palette = new List<Color4>();
            var colorToIndex = new Dictionary<Color4, int>();
            var indices = new List<int>();

            foreach (var color in pixels)
            {
                var simplifiedColor = new Color4(
                    (byte)(color.R * 255),
                    (byte)(color.G * 255),
                    (byte)(color.B * 255),
                    hasAlpha ? (byte)(color.A * 255) : (byte)255
                );

                if (!colorToIndex.TryGetValue(simplifiedColor, out int index))
                {
                    index = palette.Count;
                    palette.Add(simplifiedColor);
                    colorToIndex[simplifiedColor] = index;
                }
                indices.Add(index);
            }

            return (palette, indices);
        }

        private static void SaveWithPalette(string filePath, List<Color4> palette, List<int> indices, bool hasAlpha, SaveMode mode)
        {
            if (mode == SaveMode.All)
            {
                using var fileStream = File.Create(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
                using var binaryWriter = new BinaryWriter(gzipStream);
                WriteBinaryData(binaryWriter, palette, indices, hasAlpha, true);
                return;
            }

            if (mode == SaveMode.Text)
            {
                var sb = new StringBuilder();
                sb.Append($"1|{(int)PaletteMode.Force}|");
                sb.Append(string.Join("", palette.Select(c => ColorToHex(c, hasAlpha))));
                sb.Append("|");
                sb.Append(string.Join("", indices));

                File.WriteAllText(filePath, sb.ToString());
            }
            else
            {
                using var stream = mode == SaveMode.Compressed
                    ? (Stream)new GZipStream(File.Create(filePath), CompressionLevel.Optimal)
                    : File.Create(filePath);

                using var writer = new BinaryWriter(stream);
                WriteBinaryData(writer, palette, indices, hasAlpha, mode == SaveMode.Compressed);
            }
        }

        private static void SaveWithoutPalette(string filePath, Color4[] pixels, bool hasAlpha, SaveMode mode)
        {
            if (mode == SaveMode.All)
            {
                using var fileStream = File.Create(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
                using var binaryWriter = new BinaryWriter(gzipStream);
                WriteBinaryData(binaryWriter, null, pixels, hasAlpha, true);
                return;
            }

            if (mode == SaveMode.Text)
            {
                var sb = new StringBuilder();
                sb.Append($"1|{(int)PaletteMode.None}|");
                sb.Append(string.Join("", pixels.Select(c => ColorToHex(c, hasAlpha))));

                File.WriteAllText(filePath, sb.ToString());
            }
            else
            {
                using var stream = mode == SaveMode.Compressed
                    ? (Stream)new GZipStream(File.Create(filePath), CompressionLevel.Optimal)
                    : File.Create(filePath);

                using var writer = new BinaryWriter(stream);
                WriteBinaryData(writer, null, pixels, hasAlpha, mode == SaveMode.Compressed);
            }
        }

        private static void WriteBinaryData(BinaryWriter writer, List<Color4> palette, dynamic data, bool hasAlpha, bool compressed)
        {
            writer.Write(hasAlpha);
            writer.Write(compressed);
            writer.Write(palette != null);

            if (palette != null)
            {
                writer.Write(palette.Count);
                foreach (var color in palette)
                {
                    writer.Write((byte)(color.R * 255));
                    writer.Write((byte)(color.G * 255));
                    writer.Write((byte)(color.B * 255));
                    if (hasAlpha) writer.Write((byte)(color.A * 255));
                }

                writer.Write(data.Count);
                foreach (int index in data)
                {
                    writer.Write((ushort)index);
                }
            }
            else
            {
                writer.Write(data.Length);
                foreach (Color4 color in data)
                {
                    writer.Write((byte)(color.R * 255));
                    writer.Write((byte)(color.G * 255));
                    writer.Write((byte)(color.B * 255));
                    if (hasAlpha) writer.Write((byte)(color.A * 255));
                }
            }
        }

        private static string ColorToHex(Color4 color, bool includeAlpha)
        {
            var r = (int)(color.R * 255);
            var g = (int)(color.G * 255);
            var b = (int)(color.B * 255);

            if (includeAlpha)
            {
                var a = (int)(color.A * 255);
                return $"{r:X2}{g:X2}{b:X2}{a:X2}";
            }
            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}