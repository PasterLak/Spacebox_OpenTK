
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Spacebox.Common
{
    public class BitmapFont : IDisposable
    {
        public class Glyph
        {
            public Vector2 Size;      // Размер глифа
            public Vector2 Bearing;   // Смещение глифа относительно базовой линии
            public float Advance;     // Расстояние до следующего глифа
            public Vector2 TexOffset; // Смещение в текстуре (от 0 до 1)
            public Vector2 TexSize;   // Размер в текстуре (от 0 до 1)
        }

        private readonly Dictionary<char, Glyph> _glyphs;
        private int _texture;
        private readonly string _texturePath;
        public float LineHeight { get; private set; }

        private float _spacing;
        public float Spacing
        {
            get { return _spacing; }
             set
            {
                _spacing = value;

                foreach (var glyph in _glyphs.Values)
                {
                    glyph.Advance = _spacing;

                }
            }
        }

        public Dictionary<char, Glyph> Glyphs => _glyphs;


        // Existing constructor that loads from JSON metadata
        public BitmapFont(string metadataPath)
        {
            if (!File.Exists(metadataPath))
                throw new FileNotFoundException($"Файл метаданных шрифта не найден: {metadataPath}");

            _glyphs = new Dictionary<char, Glyph>();

            // Чтение и парсинг метаданных
            var json = File.ReadAllText(metadataPath);
            var fontData = JsonConvert.DeserializeObject<FontMetadata>(json);

            _texturePath = fontData.texturePath;

            // Загрузка текстурного атласа
            LoadTexture(_texturePath, fontData.common.textureWidth, fontData.common.textureHeight);

            // Загрузка глифов
            foreach (var kvp in fontData.characters)
            {
                char c = kvp.Key[0];
                var charData = kvp.Value;

                charData.x += 1;
                charData.y += 1;

                charData.width -= 2;
                charData.height -= 2;

                Glyph glyph = new Glyph
                {
                    Size = new Vector2(charData.width, charData.height),
                    Bearing = new Vector2(charData.xOffset, charData.yOffset),
                    Advance = charData.xAdvance,
                    TexOffset = new Vector2(charData.x / (float)fontData.common.textureWidth, charData.y / (float)fontData.common.textureHeight),
                    TexSize = new Vector2(charData.width / (float)fontData.common.textureWidth, charData.height / (float)fontData.common.textureHeight)
                };

                _glyphs.Add(c, glyph);
            }

            LineHeight = fontData.common.lineHeight;

            Console.WriteLine(_glyphs.Count);
        }

        // New constructor that loads from a fixed grid without JSON
        public BitmapFont(string texturePath, int textureWidth = 256, int textureHeight = 256, int glyphWidth = 16, int glyphHeight = 16)
        {
            if (!File.Exists(texturePath))
                throw new FileNotFoundException($"Текстура шрифта не найдена: {texturePath}");

            _glyphs = new Dictionary<char, Glyph>();
            _texturePath = texturePath;

            // Загрузка текстурного атласа
            LoadTexture(_texturePath, textureWidth, textureHeight);

            // Предопределенный массив символов
            char[,] characterGrid = GenerateCharacterGrid();

            Spacing = glyphWidth;

            int columns = textureWidth / glyphWidth;
            int rows = textureHeight / glyphHeight;

            LineHeight = glyphHeight; // Assuming line height equals glyph height

            // Предполагаемые значения смещения и аванса для моноширинного шрифта
            Vector2 defaultBearing = Vector2.Zero;
            float defaultAdvance = Spacing;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= 256)
                        break;

                    char currentChar = characterGrid[row, col];
                    if (currentChar == '\0')
                        continue; // Skip undefined characters

                    Glyph glyph = new Glyph
                    {
                        Size = new Vector2(glyphWidth, glyphHeight),
                        Bearing = new Vector2(0,8),
                        Advance = defaultAdvance,
                        TexOffset = new Vector2(col * (glyphWidth / (float)textureWidth), row * (glyphHeight / (float)textureHeight)),
                        TexSize = new Vector2(glyphWidth / (float)textureWidth, glyphHeight / (float)textureHeight)
                    };

                    if (currentChar == 'p') glyph.Bearing.Y = 11;
                    if (currentChar == '.')
                    {
                        //glyph.Bearing.X = -glyphWidth/2;
                        //glyph.Advance = 0;
                    }

                    _glyphs.Add(currentChar, glyph);
                }
            }

            Console.WriteLine($"Loaded {_glyphs.Count} glyphs from grid.");
        }

        // Generates a 16x16 grid of characters
        private char[,] GenerateCharacterGrid()
        {
            char[,] grid = new char[16, 16];

            // Define the sequence of characters
            string characters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + // 26 uppercase
                "abcdefghijklmnopqrstuvwxyz" + // 26 lowercase Aagtz
                "0123456789" +                 // 10 digits
                "!@#$%^&*()-_=+[]{}|;:',.<>/?`~\"\\ " + // 32 symbols
                "¡¢£¤¥¦§¨©ª«¬®¯°±²³´µ¶·¸¹º»¼½¾¿" + // Additional symbols
                "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞß" + // Extended Latin
                "àáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ" +
                "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚě" +
                "ĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĴĵĶķĹĺ" +
                "ĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕ" +
                "ŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲų" +
                "ŴŵŶŷŸŹźŻżŽž";

            int totalChars = characters.Length;
            int index = 0;

            for (int row = 0; row < 16; row++)
            {
                for (int col = 0; col < 16; col++)
                {
                    if (index < totalChars)
                    {
                        grid[row, col] = characters[index++];
                    }
                    else
                    {
                        grid[row, col] = '\0'; // Undefined character
                        index++;
                    }
                }
            }

            return grid;
        }

        private void LoadTexture(string path, int textureWidth, int textureHeight)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"The font texture was not found: {path}");

            using (Image<Rgba32> image = Image.Load<Rgba32>(path))
            {
                _texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _texture);

                if (image.Width != textureWidth || image.Height != textureHeight)
                {
                    image.Mutate(x => x.Resize(textureWidth, textureHeight));
                }

                byte[] pixelData = new byte[textureWidth * textureHeight * 4];
                image.CopyPixelDataTo(pixelData);

                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgba,
                              textureWidth,
                              textureHeight,
                              0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                              PixelType.UnsignedByte,
                              pixelData);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }


        public bool ContainsGlyph(char c)
        {
            return _glyphs.ContainsKey(c);
        }

        public Glyph GetGlyph(char c)
        {
            if (_glyphs.ContainsKey(c))
                return _glyphs[c];
            else
                return _glyphs.ContainsKey(' ') ? _glyphs[' '] : default;
        }

        public int Texture => _texture;

        public void Dispose()
        {
            if (_texture != 0)
            {
                GL.DeleteTexture(_texture);
                _texture = 0;
            }
        }

        // Классы для десериализации JSON
        private class FontMetadata
        {
            public string texturePath { get; set; }
            public Dictionary<string, Character> characters { get; set; }
            public Common common { get; set; }
        }

        private class Character
        {
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int xOffset { get; set; }
            public int yOffset { get; set; }
            public int xAdvance { get; set; }
        }

        private class Common
        {
            public int lineHeight { get; set; }
            public int textureWidth { get; set; }
            public int textureHeight { get; set; }
        }
    }
}
