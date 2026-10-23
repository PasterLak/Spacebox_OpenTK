using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Editor.Canvas;

public static class EditorCanvasTheme
{
    public const string FontPath = DarkTheme.FontPath;
    public const float FontSize = DarkTheme.FontSize;

    public static readonly Color4 Background = DarkTheme.Background;
    public static readonly Color4 Panel = DarkTheme.Panel;
    public static readonly Color4 Field = DarkTheme.Field;
    public static readonly Color4 FieldHover = DarkTheme.FieldHover;
    public static readonly Color4 Border = DarkTheme.Border;
    public static readonly Color4 Accent = DarkTheme.Accent;
    public static readonly Color4 AccentDark = DarkTheme.AccentDark;
    public static readonly Color4 Text = DarkTheme.Text;
    public static readonly Color4 TextDim = DarkTheme.TextDim;
    public static readonly Color4 Error = DarkTheme.Error;
    public static readonly Color4 Success = DarkTheme.Success;
    public static readonly Color4 Warning = DarkTheme.Warning;

    public static Theme Create(CanvasFont font) => DarkTheme.Create(font);
}
