using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Windows;

public enum StatusKind
{
    Normal,
    Muted,
    Good,
    Warning,
    Bad
}

public static class StatusText
{
    public static Label Create(string text = "", StatusKind kind = StatusKind.Normal)
    {
        var label = new Label();
        Set(label, text, kind);
        return label;
    }

    public static void Set(Label label, string text, StatusKind kind)
    {
        SetKind(label, kind);
        if (label.Text != text) label.Text = text ?? "";
    }

    public static void SetKind(Label label, StatusKind kind)
    {
        string variation = VariationOf(kind);
        if (label.ThemeTypeVariation != variation) label.ThemeTypeVariation = variation;
    }

    public static string VariationOf(StatusKind kind) => kind switch
    {
        StatusKind.Muted => "MutedLabel",
        StatusKind.Good => "SuccessLabel",
        StatusKind.Warning => "WarningLabel",
        StatusKind.Bad => "ErrorLabel",
        _ => ""
    };
}
