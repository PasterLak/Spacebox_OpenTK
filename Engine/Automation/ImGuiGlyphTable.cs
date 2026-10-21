using System.Runtime.InteropServices;
using ImGuiNET;

namespace Engine.Automation;

internal readonly record struct GlyphInfo(char Char, float X0, float Y0, float X1, float Y1, float U1, float V1, float AdvanceX, float FontSize, float SpaceAdvance);

internal sealed unsafe class ImGuiGlyphTable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RawGlyph
    {
        public uint Bits;
        public float AdvanceX;
        public float X0;
        public float Y0;
        public float X1;
        public float Y1;
        public float U0;
        public float V0;
        public float U1;
        public float V1;
    }

    private readonly Dictionary<long, GlyphInfo> _glyphs = new Dictionary<long, GlyphInfo>();
    private nint _textureId;
    private int _glyphCount = -1;
    private int _width;
    private int _height;

    public nint TextureId => _textureId;

    public void Refresh()
    {
        var atlas = ImGui.GetIO().Fonts;
        int total = CountGlyphs(atlas);
        if (atlas.TexID == _textureId && total == _glyphCount && atlas.TexWidth == _width && atlas.TexHeight == _height) return;

        _textureId = atlas.TexID;
        _glyphCount = total;
        _width = atlas.TexWidth;
        _height = atlas.TexHeight;
        Build(atlas);
    }

    public bool TryGet(System.Numerics.Vector2 uv, out GlyphInfo glyph)
    {
        return _glyphs.TryGetValue(Key(uv.X, uv.Y), out glyph);
    }

    private void Build(ImFontAtlasPtr atlas)
    {
        _glyphs.Clear();
        for (int f = 0; f < atlas.Fonts.Size; f++)
        {
            var font = atlas.Fonts[f];
            var vector = font.NativePtr->Glyphs;
            var raw = (RawGlyph*)vector.Data;
            float space = SpaceAdvance(raw, vector.Size);

            for (int i = 0; i < vector.Size; i++)
            {
                var g = raw[i];
                if (g.X1 <= g.X0 || g.Y1 <= g.Y0) continue;

                char c = (char)(g.Bits >> 2);
                _glyphs.TryAdd(Key(g.U0, g.V0), new GlyphInfo(c, g.X0, g.Y0, g.X1, g.Y1, g.U1, g.V1, g.AdvanceX, font.FontSize, space));
            }
        }
    }

    private static float SpaceAdvance(RawGlyph* glyphs, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if ((glyphs[i].Bits >> 2) == ' ') return glyphs[i].AdvanceX;
        }

        return 0f;
    }

    private static int CountGlyphs(ImFontAtlasPtr atlas)
    {
        int total = 0;
        for (int f = 0; f < atlas.Fonts.Size; f++) total += atlas.Fonts[f].NativePtr->Glyphs.Size;
        return total;
    }

    private long Key(float u, float v)
    {
        long x = (long)MathF.Round(u * _width * 4f);
        long y = (long)MathF.Round(v * _height * 4f);
        return (x << 32) ^ (y & 0xffffffffL);
    }
}
