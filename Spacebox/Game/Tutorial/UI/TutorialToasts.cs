using Engine.CanvasUI;
using System.Numerics;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialToasts
{
    private const float SmallDuration = 2.8f;
    private const float BigDuration = 4f;
    private const float SlideTime = 0.25f;
    private const float FadeTime = 0.5f;
    private const int MaxToasts = 4;

    private sealed class Toast
    {
        public string Title;
        public string Subtitle;
        public IReadOnlyList<TutorialReward> Rewards;
        public TutorialIcon Icon;
        public bool Big;
        public float Start;
    }

    private static readonly List<Toast> _toasts = new List<Toast>();

    public static void Add(string title, string subtitle, IReadOnlyList<TutorialReward> rewards, TutorialIcon icon, bool big, float now)
    {
        if (_toasts.Count >= MaxToasts) _toasts.RemoveAt(0);
        _toasts.Add(new Toast { Title = title, Subtitle = subtitle, Rewards = rewards ?? Array.Empty<TutorialReward>(), Icon = icon, Big = big, Start = now });
    }

    public static void Clear()
    {
        _toasts.Clear();
    }

    public static void Draw(TutorialCanvas drawList, Vector2 anchorTopRight, Vector2 display, float width, float baseSize, float now)
    {
        var font = drawList.Font;
        float y = anchorTopRight.Y;

        for (int i = 0; i < _toasts.Count; i++)
        {
            var toast = _toasts[i];
            float age = now - toast.Start;
            float duration = toast.Big ? BigDuration : SmallDuration;
            if (age > duration)
            {
                _toasts.RemoveAt(i--);
                continue;
            }

            float alpha = Math.Min(TutorialDraw.EaseOut(age / SlideTime), Math.Clamp((duration - age) / FadeTime, 0f, 1f));
            if (toast.Big) DrawBig(drawList, font, toast, display, baseSize, alpha, age);
            else y += DrawSmall(drawList, font, toast, new Vector2(anchorTopRight.X, y), width, baseSize, alpha, age) + baseSize * 0.4f;
        }
    }

    private static float DrawSmall(TutorialCanvas drawList, CanvasFont font, Toast toast, Vector2 topRight, float width, float baseSize, float alpha, float age)
    {
        float pad = baseSize * 0.55f;
        float lineH = baseSize * 1.25f;
        float iconSize = lineH * 1.2f;
        bool hasRewards = toast.Rewards.Count > 0;
        float height = pad * 2 + lineH + (string.IsNullOrEmpty(toast.Subtitle) ? 0 : lineH) + (hasRewards ? iconSize + pad * 0.5f : 0);
        float slide = (1f - TutorialDraw.EaseOut(age / SlideTime)) * width * 0.6f;
        var min = new Vector2(topRight.X - width + slide, topRight.Y);
        var max = min + new Vector2(width, height);

        drawList.RectFilled(min, max, TutorialDraw.WithAlpha(TutorialDraw.PanelBackground, alpha), baseSize * 0.35f);
        drawList.RectFilled(min, new Vector2(min.X + baseSize * 0.25f, max.Y), TutorialDraw.WithAlpha(TutorialDraw.Green, alpha));

        var cursor = min + new Vector2(pad * 1.4f, pad);
        drawList.Text(baseSize * 1.05f, cursor, TutorialDraw.WithAlpha(TutorialDraw.Green, alpha), toast.Title);
        cursor.Y += lineH;

        if (!string.IsNullOrEmpty(toast.Subtitle))
        {
            drawList.Text(baseSize, cursor, TutorialDraw.WithAlpha(TutorialDraw.TextDim, alpha), TutorialDraw.Truncate(font, baseSize, toast.Subtitle, width - pad * 3f));
            cursor.Y += lineH;
        }

        if (hasRewards) DrawRewards(drawList, font, toast.Rewards, cursor + new Vector2(0, pad * 0.25f), iconSize, baseSize, alpha);
        return height;
    }

    private static void DrawBig(TutorialCanvas drawList, CanvasFont font, Toast toast, Vector2 display, float baseSize, float alpha, float age)
    {
        float titleSize = baseSize * 1.9f;
        float scale = 0.85f + 0.15f * TutorialDraw.EaseOut(age / 0.4f);
        float iconSize = baseSize * 3f * scale;
        float titleWidth = TutorialDraw.Measure(font, titleSize * scale, toast.Title);
        float subtitleWidth = TutorialDraw.Measure(font, baseSize * 1.1f, toast.Subtitle);
        float rewardsWidth = toast.Rewards.Count * baseSize * 4.2f;
        float width = MathF.Max(MathF.Max(titleWidth, subtitleWidth), rewardsWidth) + iconSize + baseSize * 4f;
        float height = iconSize + baseSize * 2.6f + (toast.Rewards.Count > 0 ? baseSize * 2.4f : 0f);
        var min = new Vector2((display.X - width) * 0.5f, display.Y * 0.16f);
        var max = min + new Vector2(width, height);

        drawList.Panel(min, max, TutorialDraw.WithAlpha(TutorialDraw.PanelBackground, alpha), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), baseSize * 0.6f, MathF.Max(2f, baseSize * 0.15f));

        float x = min.X + baseSize * 1.5f;
        float y = min.Y + baseSize * 1.3f;
        if (!toast.Icon.IsEmpty)
        {
            TutorialDraw.Icon(drawList, toast.Icon, new Vector2(x, y), iconSize, alpha);
            x += iconSize + baseSize;
        }

        drawList.Text(titleSize * scale, new Vector2(x, y), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), toast.Title);
        drawList.Text(baseSize * 1.1f, new Vector2(x, y + titleSize * scale * 1.15f), TutorialDraw.WithAlpha(TutorialDraw.TextBright, alpha), toast.Subtitle ?? "");

        if (toast.Rewards.Count > 0)
            DrawRewards(drawList, font, toast.Rewards, new Vector2(x, y + iconSize + baseSize * 0.6f), baseSize * 1.8f, baseSize * 1.05f, alpha);
    }

    private static void DrawRewards(TutorialCanvas drawList, CanvasFont font, IReadOnlyList<TutorialReward> rewards, Vector2 position, float iconSize, float textSize, float alpha)
    {
        drawList.Text(textSize, position + new Vector2(0, (iconSize - textSize) * 0.5f), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), "Reward:");
        float x = position.X + TutorialDraw.Measure(font, textSize, "Reward:") + textSize * 0.6f;

        for (int i = 0; i < rewards.Count; i++)
        {
            TutorialDraw.Icon(drawList, new TutorialIcon(rewards[i].ItemId, null), new Vector2(x, position.Y), iconSize, alpha);
            x += iconSize + textSize * 0.2f;
            string count = "x" + rewards[i].Count;
            drawList.Text(textSize, new Vector2(x, position.Y + (iconSize - textSize) * 0.5f), TutorialDraw.WithAlpha(TutorialDraw.TextBright, alpha), count);
            x += TutorialDraw.Measure(font, textSize, count) + textSize * 0.8f;
        }
    }
}
