using Engine;
using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas;

public sealed class InterfaceFontSlot : IDisposable
{
    private string _id;
    private int _size;
    private SkiaCanvasFont _font;

    public SkiaCanvasFont Font => _font;

    public static bool Differs(string currentId, int currentSize, string id, int size)
    {
        return !string.Equals(currentId, id, StringComparison.OrdinalIgnoreCase) || currentSize != size;
    }

    public bool Update(InterfaceSettings settings, out SkiaCanvasFont previous)
    {
        previous = null;
        if (Application.IsHeadless || settings == null) return false;

        var info = InterfaceFonts.Find(settings.Font);
        int size = InterfaceFonts.SizeFor(info, settings.FontSize);
        if (_font != null && !Differs(_id, _size, info.Id, size)) return false;

        previous = _font;
        _font = InterfaceFonts.CreateCanvasFont(info.Id, size);
        _id = info.Id;
        _size = size;
        return true;
    }

    public void Dispose()
    {
        _font?.Dispose();
        _font = null;
        _id = null;
    }
}
