using System.Text;

namespace Engine.CanvasUI.Validation;

public static class ControlPath
{
    public static string Of(CanvasItem item)
    {
        var segments = new List<string>();
        for (var cursor = item; cursor != null && cursor.Parent != null; cursor = cursor.Parent) segments.Add(Segment(cursor));
        segments.Reverse();
        return string.Join("/", segments);
    }

    private static string Segment(CanvasItem item)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(item.Name)) builder.Append(item.Name);
        else builder.Append(item.GetType().Name).Append('#').Append(item.Parent.IndexOf(item));

        if (item is Control control && !string.IsNullOrEmpty(control.ThemeTypeVariation)) builder.Append('(').Append(control.ThemeTypeVariation).Append(')');
        if (item is Label label && label.Text.Length > 0) builder.Append(" '").Append(Shorten(label.Text)).Append('\'');
        if (item is Button button && button.Text.Length > 0) builder.Append(" '").Append(Shorten(button.Text)).Append('\'');
        return builder.ToString();
    }

    private static string Shorten(string text)
    {
        text = text.Replace('\n', ' ');
        return text.Length <= 24 ? text : text.Substring(0, 23) + "…";
    }
}
