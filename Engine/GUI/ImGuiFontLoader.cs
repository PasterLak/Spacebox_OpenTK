using ImGuiNET;

namespace Engine.GUI;

public static class ImGuiFontLoader
{
    public static unsafe bool HasGlyph(ImFontPtr font, char letter)
    {
        return font.NativePtr != null && font.FindGlyphNoFallback(letter).NativePtr != null;
    }

    public static unsafe void Load(ImFontAtlasPtr fonts, ImGuiFontRequest request)
    {
        fonts.Clear();

        var config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
        try
        {
            config.OversampleH = 1;
            config.OversampleV = 1;
            config.PixelSnapH = true;
            config.SizePixels = request.SizePixels;

            if (!request.IsBuiltIn && File.Exists(request.FilePath))
            {
                fonts.AddFontFromFileTTF(request.FilePath, request.SizePixels, config, fonts.GetGlyphRangesCyrillic());
                return;
            }

            if (!request.IsBuiltIn) Debug.Warning($"[ImGuiFontLoader] Font file not found, using the built-in font: {request.FilePath}");
            fonts.AddFontDefault(config);
        }
        finally
        {
            config.Destroy();
        }
    }
}
