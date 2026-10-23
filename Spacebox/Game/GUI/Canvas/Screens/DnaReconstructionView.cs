using Engine.CanvasUI;
using OpenTK.Mathematics;
using Screen = Spacebox.Game.GUI.DnaReconstructionScreen;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class DnaReconstructionView : GameScreen
{
    private const int RibSegments = 8;
    private const float TextSize = 24f;

    private static readonly Color4 BackgroundColor = new Color4(0.01f, 0.03f, 0.06f, 1f);
    private static readonly Color4 GridColor = new Color4(0.1f, 0.45f, 0.7f, 0.12f);
    private static readonly Color4 BoneColor = new Color4(0.85f, 0.97f, 1f, 1f);
    private static readonly Color4 BoneGlowColor = new Color4(0.3f, 0.8f, 1f, 0.25f);
    private static readonly Color4 MuscleColor = new Color4(0.85f, 0.15f, 0.2f, 0.6f);
    private static readonly Color4 SkinColor = new Color4(0.95f, 0.78f, 0.66f, 1f);
    private static readonly Color4 ScanColor = new Color4(0.4f, 0.95f, 1f, 1f);
    private static readonly Color4 HelixColorA = new Color4(0.3f, 0.85f, 1f, 0.8f);
    private static readonly Color4 HelixColorB = new Color4(1f, 0.85f, 0.1f, 0.8f);
    private static readonly Color4 TextColor = new Color4(0.6f, 0.95f, 1f, 1f);
    private static readonly Color4 BarColor = new Color4(1f, 0.85f, 0.1f, 1f);

    private readonly string[] _percentTexts = new string[101];

    private readonly Screen _reconstruction;

    public DnaReconstructionView(Screen reconstruction)
    {
        _reconstruction = reconstruction;
        Name = "DnaReconstruction";
        MouseFilter = MouseFilter.Ignore;
        for (int i = 0; i < _percentTexts.Length; i++) _percentTexts[i] = i + "%";
    }

    public override bool IsShown => _reconstruction.IsActive;

    protected override void Draw(ICanvasRenderer renderer)
    {
        var size = Size;
        float t = _reconstruction.Progress;
        float fadeIn = Math.Clamp(t / Screen.FadeInEnd, 0f, 1f);

        renderer.DrawRect(new Rect2(Vector2.Zero, size), Fade(BackgroundColor, fadeIn));
        DrawGrid(renderer, size, fadeIn);
        DrawHelix(renderer, new Vector2(size.X * 0.15f, size.Y * 0.5f), size.Y * 0.7f, size.Y * 0.05f, fadeIn);
        DrawHelix(renderer, new Vector2(size.X * 0.85f, size.Y * 0.5f), size.Y * 0.7f, size.Y * 0.05f, fadeIn);

        var body = new BodyFrame(new Vector2(size.X * 0.5f, size.Y * 0.88f), size.Y * 0.75f);
        DrawBody(renderer, body, size, t);
        DrawTexts(renderer, size, t, fadeIn);

        float flash = Math.Clamp((t - Screen.SkinEnd) / (1f - Screen.SkinEnd), 0f, 1f);
        if (flash > 0f) renderer.DrawRect(new Rect2(Vector2.Zero, size), Fade(Color4.White, flash * flash));
    }

    private static void DrawBody(ICanvasRenderer renderer, BodyFrame body, Vector2 size, float t)
    {
        float skeleton = Screen.Phase(t, Screen.FadeInEnd, Screen.SkeletonEnd);
        float muscle = Screen.Phase(t, Screen.SkeletonEnd, Screen.MuscleEnd);
        float skin = Screen.Phase(t, Screen.MuscleEnd, Screen.SkinEnd);
        float thickness = body.Height * Screen.BoneThickness;

        if (skin > 0f) DrawLayer(renderer, body, size, skin, thickness, 5.2f, Fade(SkinColor, Math.Min(1f, skin * 1.2f)), 0.075f, false);
        if (muscle > 0f) DrawLayer(renderer, body, size, muscle, thickness, 1f, Fade(MuscleColor, 1f - skin * 0.8f), 0.068f, true);

        float boneAlpha = 1f - skin;
        if (skeleton > 0f && boneAlpha > 0f) DrawSkeleton(renderer, body, size, skeleton, thickness, boneAlpha);

        if (skeleton > 0f && skeleton < 1f) DrawScanLine(renderer, body, skeleton);
        if (muscle > 0f && muscle < 1f) DrawScanLine(renderer, body, muscle);
        if (skin > 0f && skin < 1f) DrawScanLine(renderer, body, skin);
    }

    private static void DrawSkeleton(ICanvasRenderer renderer, BodyFrame body, Vector2 size, float reveal, float thickness, float alpha)
    {
        float top = body.ScanY(reveal);
        renderer.PushClip(new Rect2(0f, top, size.X, size.Y - top));

        var glow = Fade(BoneGlowColor, alpha);
        var bone = Fade(BoneColor, alpha);

        for (int i = 0; i < Screen.Bones.Length; i++)
        {
            var a = body.Point(Screen.Joints[Screen.Bones[i].A]);
            var b = body.Point(Screen.Joints[Screen.Bones[i].B]);
            CanvasShapes.Line(renderer, a, b, glow, thickness * 3f);
            CanvasShapes.Line(renderer, a, b, bone, thickness);
            CanvasShapes.FilledCircle(renderer, a, thickness * 0.9f, bone);
            CanvasShapes.FilledCircle(renderer, b, thickness * 0.9f, bone);
        }

        for (int i = 0; i < Screen.RibHeights.Length; i++)
        {
            float width = 0.1f - i * 0.008f;
            DrawRib(renderer, body, Screen.RibHeights[i], -width, bone, thickness * 0.6f);
            DrawRib(renderer, body, Screen.RibHeights[i], width, bone, thickness * 0.6f);
        }

        CanvasShapes.Line(renderer, body.Point(new Vector2(-0.09f, 0.53f)), body.Point(new Vector2(0.09f, 0.53f)), bone, thickness * 1.4f);

        var head = body.Point(Screen.Joints[0]);
        float skull = body.Height * 0.055f;
        CanvasShapes.Circle(renderer, head, skull, glow, thickness * 3f);
        CanvasShapes.Circle(renderer, head, skull, bone, thickness);
        CanvasShapes.FilledCircle(renderer, head + new Vector2(-skull * 0.35f, -skull * 0.1f), skull * 0.18f, bone);
        CanvasShapes.FilledCircle(renderer, head + new Vector2(skull * 0.35f, -skull * 0.1f), skull * 0.18f, bone);

        renderer.PopClip();
    }

    private static void DrawRib(ICanvasRenderer renderer, BodyFrame body, float height, float width, Color4 color, float thickness)
    {
        var start = body.Point(new Vector2(0f, height));
        var control = body.Point(new Vector2(width * 1.3f, height + 0.015f));
        var end = body.Point(new Vector2(width, height - 0.03f));
        var previous = start;

        for (int i = 1; i <= RibSegments; i++)
        {
            float k = i / (float)RibSegments;
            var point = (1f - k) * (1f - k) * start + 2f * (1f - k) * k * control + k * k * end;
            CanvasShapes.Line(renderer, previous, point, color, thickness);
            previous = point;
        }
    }

    private static void DrawLayer(ICanvasRenderer renderer, BodyFrame body, Vector2 size, float reveal, float thickness, float scale, Color4 color, float headRadius, bool perBoneWidth)
    {
        float top = body.ScanY(reveal);
        renderer.PushClip(new Rect2(0f, top, size.X, size.Y - top));

        for (int i = 0; i < Screen.Bones.Length; i++)
        {
            float width = thickness * (perBoneWidth ? Screen.Bones[i].Muscle * 2.2f : scale);
            var a = body.Point(Screen.Joints[Screen.Bones[i].A]);
            var b = body.Point(Screen.Joints[Screen.Bones[i].B]);
            CanvasShapes.Line(renderer, a, b, color, width);
            CanvasShapes.FilledCircle(renderer, a, width * 0.5f, color);
            CanvasShapes.FilledCircle(renderer, b, width * 0.5f, color);
        }

        CanvasShapes.FilledCircle(renderer, body.Point(Screen.Joints[0]), body.Height * headRadius, color);
        renderer.PopClip();
    }

    private static void DrawScanLine(ICanvasRenderer renderer, BodyFrame body, float reveal)
    {
        float y = body.ScanY(reveal);
        float halfWidth = body.Height * 0.3f;
        float band = body.Height * 0.02f;
        renderer.DrawRect(new Rect2(body.Feet.X - halfWidth, y - band, halfWidth * 2f, band * 2f), Fade(ScanColor, 0.12f));
        renderer.DrawRect(new Rect2(body.Feet.X - halfWidth, y - 1f, halfWidth * 2f, 2f), ScanColor);
    }

    private void DrawGrid(ICanvasRenderer renderer, Vector2 size, float alpha)
    {
        var color = Fade(GridColor, alpha);
        float step = size.Y / Screen.GridLines;
        float offset = (_reconstruction.Elapsed * step * 0.5f) % step;

        for (float y = offset; y < size.Y; y += step) renderer.DrawRect(new Rect2(0f, y, size.X, 1f), color);
        for (float x = 0f; x < size.X; x += step) renderer.DrawRect(new Rect2(x, 0f, 1f, size.Y), color);
    }

    private void DrawHelix(ICanvasRenderer renderer, Vector2 center, float height, float radius, float alpha)
    {
        var colorA = Fade(HelixColorA, alpha);
        var colorB = Fade(HelixColorB, alpha);
        var rung = Fade(GridColor, alpha * 4f);
        float phase = _reconstruction.Elapsed * 2.5f;
        float top = center.Y - height * 0.5f;

        Vector2 previousA = default, previousB = default;
        for (int i = 0; i <= Screen.HelixSegments; i++)
        {
            float k = i / (float)Screen.HelixSegments;
            float angle = k * MathF.PI * 6f + phase;
            float y = top + k * height;
            var a = new Vector2(center.X + MathF.Sin(angle) * radius, y);
            var b = new Vector2(center.X - MathF.Sin(angle) * radius, y);

            if (i > 0)
            {
                CanvasShapes.Line(renderer, previousA, a, colorA, 2f);
                CanvasShapes.Line(renderer, previousB, b, colorB, 2f);
            }
            if (i % 3 == 0) CanvasShapes.Line(renderer, a, b, rung, 1f);

            previousA = a;
            previousB = b;
        }
    }

    private void DrawTexts(ICanvasRenderer renderer, Vector2 size, float t, float alpha)
    {
        var font = GetThemeFont();
        string step = Screen.Steps[Screen.StepIndex(t)].Text;
        var textColor = Fade(TextColor, alpha);
        float lineHeight = font != null ? font.LineHeight * TextSize / font.BaseSize : TextSize;
        var origin = new Vector2(size.X * 0.04f, size.Y * 0.06f);

        CanvasDraw.Text(renderer, font, origin, "DNA RECONSTRUCTOR", TextSize, Fade(BarColor, alpha));
        CanvasDraw.Text(renderer, font, origin + new Vector2(0f, lineHeight * 1.5f), step, TextSize, textColor);
        CanvasDraw.Text(renderer, font, origin + new Vector2(0f, lineHeight * 3f), _percentTexts[(int)(t * 100f)], TextSize, textColor);

        var bar = new Rect2(size.X * 0.3f, size.Y * 0.94f, size.X * 0.4f, MathF.Max(3f, size.Y * 0.008f));
        renderer.DrawRect(bar, Fade(GridColor, alpha * 3f));
        renderer.DrawRect(new Rect2(bar.X, bar.Y, bar.Width * t, bar.Height), Fade(BarColor, alpha));
    }

    private static Color4 Fade(Color4 color, float alpha) => new Color4(color.R, color.G, color.B, color.A * Math.Clamp(alpha, 0f, 1f));

    private readonly struct BodyFrame
    {
        public readonly Vector2 Feet;
        public readonly float Height;

        public BodyFrame(Vector2 feet, float height)
        {
            Feet = feet;
            Height = height;
        }

        public Vector2 Point(Vector2 normalized) => new Vector2(Feet.X + normalized.X * Height, Feet.Y - normalized.Y * Height);

        public float ScanY(float reveal) => Feet.Y - reveal * Height * 1.02f;
    }
}
