namespace Engine.CanvasUI.Validation;

public sealed class TextCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        var rules = context.Rules;
        float tolerance = rules.Tolerance;

        for (int i = 0; i < context.Entries.Count; i++)
        {
            var control = context.Entries[i].Control;
            if (rules.IsIgnored(control)) continue;

            if (control is Label label && label.Text.Length > 0)
            {
                CheckFontSize(context, label, issues);
                if (label.Overrun != TextOverrun.None || label.ClipContents) continue;
                var text = label.GetTextSize();
                if (text.X > label.Size.X + tolerance || text.Y > label.Size.Y + tolerance)
                    issues.Add(new LayoutIssue(LayoutIssueKind.TextOverflow, LayoutSeverity.Error, label, $"text needs {text.X:0}x{text.Y:0}"));
            }
            else if (control is Button button && (button.Text.Length > 0 || button.Icon != null))
            {
                if (button.Text.Length > 0) CheckFontSize(context, button, issues);
                var content = button.GetContentSize();
                var inner = button.ContentRect;
                if (content.X > inner.Width + tolerance || content.Y > inner.Height + tolerance)
                    issues.Add(new LayoutIssue(LayoutIssueKind.TextOverflow, LayoutSeverity.Error, button, $"content needs {content.X:0}x{content.Y:0}, has {inner.Width:0}x{inner.Height:0}"));
            }
        }
    }

    private static void CheckFontSize(LayoutContext context, Control control, List<LayoutIssue> issues)
    {
        float pixels = control.GetThemeFontSize() * context.Root.Scale;
        if (pixels + 0.01f < context.Rules.MinFontPixels)
            issues.Add(new LayoutIssue(LayoutIssueKind.SmallText, LayoutSeverity.Error, control, $"text is {pixels:0.#} px on screen, minimum {context.Rules.MinFontPixels:0.#} px"));
    }
}
