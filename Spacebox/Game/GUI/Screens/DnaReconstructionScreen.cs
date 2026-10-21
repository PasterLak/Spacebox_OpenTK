using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.Player.Respawn;

namespace Spacebox.Game.GUI;

public static class DnaReconstructionScreen
{
    private const float SkeletonEnd = 0.35f;
    private const float MuscleEnd = 0.62f;
    private const float SkinEnd = 0.88f;
    private const float FadeInEnd = 0.06f;
    private const float FlashFadeDuration = 0.8f;
    private const float BoneThickness = 0.012f;
    private const int HelixSegments = 48;
    private const int GridLines = 24;

    private static readonly Vector4 BackgroundColor = new Vector4(0.01f, 0.03f, 0.06f, 1f);
    private static readonly Vector4 GridColor = new Vector4(0.1f, 0.45f, 0.7f, 0.12f);
    private static readonly Vector4 BoneColor = new Vector4(0.85f, 0.97f, 1f, 1f);
    private static readonly Vector4 BoneGlowColor = new Vector4(0.3f, 0.8f, 1f, 0.25f);
    private static readonly Vector4 MuscleColor = new Vector4(0.85f, 0.15f, 0.2f, 0.6f);
    private static readonly Vector4 SkinColor = new Vector4(0.95f, 0.78f, 0.66f, 1f);
    private static readonly Vector4 ScanColor = new Vector4(0.4f, 0.95f, 1f, 1f);
    private static readonly Vector4 HelixColorA = new Vector4(0.3f, 0.85f, 1f, 0.8f);
    private static readonly Vector4 HelixColorB = new Vector4(1f, 0.85f, 0.1f, 0.8f);
    private static readonly Vector4 TextColor = new Vector4(0.6f, 0.95f, 1f, 1f);
    private static readonly Vector4 BarColor = new Vector4(1f, 0.85f, 0.1f, 1f);

    private static readonly Vector2[] Joints =
    {
        new Vector2(0f, 0.93f),
        new Vector2(0f, 0.86f),
        new Vector2(0f, 0.78f),
        new Vector2(0f, 0.65f),
        new Vector2(0f, 0.53f),
        new Vector2(-0.13f, 0.82f),
        new Vector2(0.13f, 0.82f),
        new Vector2(-0.17f, 0.66f),
        new Vector2(0.17f, 0.66f),
        new Vector2(-0.19f, 0.52f),
        new Vector2(0.19f, 0.52f),
        new Vector2(-0.2f, 0.47f),
        new Vector2(0.2f, 0.47f),
        new Vector2(-0.07f, 0.5f),
        new Vector2(0.07f, 0.5f),
        new Vector2(-0.08f, 0.27f),
        new Vector2(0.08f, 0.27f),
        new Vector2(-0.08f, 0.04f),
        new Vector2(0.08f, 0.04f),
        new Vector2(-0.12f, 0f),
        new Vector2(0.12f, 0f)
    };

    private static readonly (int A, int B, float Muscle)[] Bones =
    {
        (1, 2, 2.2f), (2, 3, 4.5f), (3, 4, 4f),
        (2, 5, 2.5f), (2, 6, 2.5f),
        (5, 7, 2.6f), (6, 8, 2.6f), (7, 9, 2.1f), (8, 10, 2.1f), (9, 11, 1.6f), (10, 12, 1.6f),
        (4, 13, 3f), (4, 14, 3f), (13, 15, 3.2f), (14, 16, 3.2f), (15, 17, 2.4f), (16, 18, 2.4f), (17, 19, 1.5f), (18, 20, 1.5f)
    };

    private static readonly float[] RibHeights = { 0.8f, 0.76f, 0.72f, 0.68f };

    private static readonly (float Start, string Text)[] Steps =
    {
        (0f, "SEQUENCING DNA"),
        (FadeInEnd, "RECONSTRUCTING SKELETAL STRUCTURE"),
        (SkeletonEnd, "GROWING MUSCLE TISSUE"),
        (MuscleEnd, "RESTORING NERVOUS SYSTEM AND SKIN"),
        (SkinEnd, "RECONSTRUCTION COMPLETE")
    };

    private static float _duration = 5f;
    private static float _elapsed;
    private static Action _onComplete;
    private static IReconstructionEffects _effects;

    public static bool IsActive { get; private set; }

    public static void Play(float duration, Action onComplete, IReconstructionEffects effects = null)
    {
        _effects?.End();
        _duration = MathF.Max(0.1f, duration);
        _elapsed = 0f;
        _onComplete = onComplete;
        _effects = effects;
        IsActive = true;
        _effects?.Begin();
        Input.HideCursor();
    }

    public static void Update()
    {
        if (!IsActive) return;

        _elapsed += Time.Delta;
        float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
        _effects?.Update(t, StepIndex(t));
        if (_elapsed < _duration) return;

        var onComplete = _onComplete;
        Reset();
        onComplete?.Invoke();
        ColorOverlay.FadeOut(Vector3.One, 1f, FlashFadeDuration);
    }

    public static void OnGUI()
    {
        if (!IsActive) return;

        float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
        var drawList = ImGui.GetForegroundDrawList();
        var size = ImGui.GetIO().DisplaySize;
        float fadeIn = Math.Clamp(t / FadeInEnd, 0f, 1f);

        drawList.AddRectFilled(Vector2.Zero, size, Color(BackgroundColor, fadeIn));
        DrawGrid(drawList, size, fadeIn);
        DrawHelix(drawList, new Vector2(size.X * 0.15f, size.Y * 0.5f), size.Y * 0.7f, size.Y * 0.05f, fadeIn);
        DrawHelix(drawList, new Vector2(size.X * 0.85f, size.Y * 0.5f), size.Y * 0.7f, size.Y * 0.05f, fadeIn);

        var body = new BodyFrame(new Vector2(size.X * 0.5f, size.Y * 0.88f), size.Y * 0.75f);
        DrawBody(drawList, body, size, t);
        DrawTexts(drawList, size, t, fadeIn);

        float flash = Math.Clamp((t - SkinEnd) / (1f - SkinEnd), 0f, 1f);
        if (flash > 0f) drawList.AddRectFilled(Vector2.Zero, size, Color(Vector4.One, flash * flash));
    }

    public static void Reset()
    {
        _effects?.End();
        _effects = null;
        IsActive = false;
        _onComplete = null;
        _elapsed = 0f;
    }

    private static void DrawBody(ImDrawListPtr drawList, BodyFrame body, Vector2 size, float t)
    {
        float skeleton = Phase(t, FadeInEnd, SkeletonEnd);
        float muscle = Phase(t, SkeletonEnd, MuscleEnd);
        float skin = Phase(t, MuscleEnd, SkinEnd);
        float thickness = body.Height * BoneThickness;

        if (skin > 0f) DrawLayer(drawList, body, size, skin, thickness, 5.2f, Color(SkinColor, Math.Min(1f, skin * 1.2f)), 0.075f);
        if (muscle > 0f) DrawLayer(drawList, body, size, muscle, thickness, 1f, Color(MuscleColor, 1f - skin * 0.8f), 0.068f, true);

        float boneAlpha = 1f - skin;
        if (skeleton > 0f && boneAlpha > 0f) DrawSkeleton(drawList, body, size, skeleton, thickness, boneAlpha);

        if (skeleton > 0f && skeleton < 1f) DrawScanLine(drawList, body, size, skeleton);
        if (muscle > 0f && muscle < 1f) DrawScanLine(drawList, body, size, muscle);
        if (skin > 0f && skin < 1f) DrawScanLine(drawList, body, size, skin);
    }

    private static void DrawSkeleton(ImDrawListPtr drawList, BodyFrame body, Vector2 size, float reveal, float thickness, float alpha)
    {
        drawList.PushClipRect(new Vector2(0f, body.ScanY(reveal)), size, false);

        uint glow = Color(BoneGlowColor, alpha);
        uint bone = Color(BoneColor, alpha);

        for (int i = 0; i < Bones.Length; i++)
        {
            var a = body.Point(Joints[Bones[i].A]);
            var b = body.Point(Joints[Bones[i].B]);
            drawList.AddLine(a, b, glow, thickness * 3f);
            drawList.AddLine(a, b, bone, thickness);
            drawList.AddCircleFilled(a, thickness * 0.9f, bone);
            drawList.AddCircleFilled(b, thickness * 0.9f, bone);
        }

        for (int i = 0; i < RibHeights.Length; i++)
        {
            float width = 0.1f - i * 0.008f;
            DrawRib(drawList, body, RibHeights[i], -width, bone, thickness * 0.6f);
            DrawRib(drawList, body, RibHeights[i], width, bone, thickness * 0.6f);
        }

        var pelvisLeft = body.Point(new Vector2(-0.09f, 0.53f));
        var pelvisRight = body.Point(new Vector2(0.09f, 0.53f));
        drawList.AddLine(pelvisLeft, pelvisRight, bone, thickness * 1.4f);

        var head = body.Point(Joints[0]);
        float skull = body.Height * 0.055f;
        drawList.AddCircle(head, skull, glow, 24, thickness * 3f);
        drawList.AddCircle(head, skull, bone, 24, thickness);
        drawList.AddCircleFilled(head + new Vector2(-skull * 0.35f, -skull * 0.1f), skull * 0.18f, bone);
        drawList.AddCircleFilled(head + new Vector2(skull * 0.35f, -skull * 0.1f), skull * 0.18f, bone);

        drawList.PopClipRect();
    }

    private static void DrawRib(ImDrawListPtr drawList, BodyFrame body, float height, float width, uint color, float thickness)
    {
        var start = body.Point(new Vector2(0f, height));
        var control = body.Point(new Vector2(width * 1.3f, height + 0.015f));
        var end = body.Point(new Vector2(width, height - 0.03f));
        var previous = start;

        for (int i = 1; i <= 8; i++)
        {
            float k = i / 8f;
            var point = (1f - k) * (1f - k) * start + 2f * (1f - k) * k * control + k * k * end;
            drawList.AddLine(previous, point, color, thickness);
            previous = point;
        }
    }

    private static void DrawLayer(ImDrawListPtr drawList, BodyFrame body, Vector2 size, float reveal, float thickness, float scale, uint color, float headRadius, bool perBoneWidth = false)
    {
        drawList.PushClipRect(new Vector2(0f, body.ScanY(reveal)), size, false);

        for (int i = 0; i < Bones.Length; i++)
        {
            float width = thickness * (perBoneWidth ? Bones[i].Muscle * 2.2f : scale);
            var a = body.Point(Joints[Bones[i].A]);
            var b = body.Point(Joints[Bones[i].B]);
            drawList.AddLine(a, b, color, width);
            drawList.AddCircleFilled(a, width * 0.5f, color);
            drawList.AddCircleFilled(b, width * 0.5f, color);
        }

        drawList.AddCircleFilled(body.Point(Joints[0]), body.Height * headRadius, color, 24);
        drawList.PopClipRect();
    }

    private static void DrawScanLine(ImDrawListPtr drawList, BodyFrame body, Vector2 size, float reveal)
    {
        float y = body.ScanY(reveal);
        float halfWidth = body.Height * 0.3f;
        var left = new Vector2(body.Feet.X - halfWidth, y);
        var right = new Vector2(body.Feet.X + halfWidth, y);

        drawList.AddRectFilled(left - new Vector2(0f, body.Height * 0.02f), right + new Vector2(0f, body.Height * 0.02f), Color(ScanColor, 0.12f));
        drawList.AddLine(left, right, Color(ScanColor, 1f), 2f);
    }

    private static void DrawGrid(ImDrawListPtr drawList, Vector2 size, float alpha)
    {
        uint color = Color(GridColor, alpha);
        float step = size.Y / GridLines;
        float offset = (_elapsed * step * 0.5f) % step;

        for (float y = offset; y < size.Y; y += step)
            drawList.AddLine(new Vector2(0f, y), new Vector2(size.X, y), color);

        for (float x = 0f; x < size.X; x += step)
            drawList.AddLine(new Vector2(x, 0f), new Vector2(x, size.Y), color);
    }

    private static void DrawHelix(ImDrawListPtr drawList, Vector2 center, float height, float radius, float alpha)
    {
        uint colorA = Color(HelixColorA, alpha);
        uint colorB = Color(HelixColorB, alpha);
        uint rung = Color(GridColor, alpha * 4f);
        float phase = _elapsed * 2.5f;
        float top = center.Y - height * 0.5f;

        Vector2 previousA = default, previousB = default;
        for (int i = 0; i <= HelixSegments; i++)
        {
            float k = i / (float)HelixSegments;
            float angle = k * MathF.PI * 6f + phase;
            float y = top + k * height;
            var a = new Vector2(center.X + MathF.Sin(angle) * radius, y);
            var b = new Vector2(center.X - MathF.Sin(angle) * radius, y);

            if (i > 0)
            {
                drawList.AddLine(previousA, a, colorA, 2f);
                drawList.AddLine(previousB, b, colorB, 2f);
            }
            if (i % 3 == 0) drawList.AddLine(a, b, rung, 1f);

            previousA = a;
            previousB = b;
        }
    }

    private static int StepIndex(float t)
    {
        int index = 0;
        for (int i = 0; i < Steps.Length; i++)
        {
            if (t >= Steps[i].Start) index = i;
        }
        return index;
    }

    private static void DrawTexts(ImDrawListPtr drawList, Vector2 size, float t, float alpha)
    {
        string step = Steps[StepIndex(t)].Text;

        uint textColor = Color(TextColor, alpha);
        float lineHeight = ImGui.GetTextLineHeight();
        var origin = new Vector2(size.X * 0.04f, size.Y * 0.06f);

        drawList.AddText(origin, Color(BarColor, alpha), "DNA RECONSTRUCTOR");
        drawList.AddText(origin + new Vector2(0f, lineHeight * 1.5f), textColor, step);
        drawList.AddText(origin + new Vector2(0f, lineHeight * 3f), textColor, $"{(int)(t * 100f)}%");

        var barMin = new Vector2(size.X * 0.3f, size.Y * 0.94f);
        var barSize = new Vector2(size.X * 0.4f, size.Y * 0.008f);
        drawList.AddRectFilled(barMin, barMin + barSize, Color(GridColor, alpha * 3f));
        drawList.AddRectFilled(barMin, barMin + new Vector2(barSize.X * t, barSize.Y), Color(BarColor, alpha));
    }

    private static float Phase(float t, float start, float end)
    {
        return Math.Clamp((t - start) / (end - start), 0f, 1f);
    }

    private static uint Color(Vector4 color, float alpha)
    {
        return ImGui.ColorConvertFloat4ToU32(new Vector4(color.X, color.Y, color.Z, color.W * Math.Clamp(alpha, 0f, 1f)));
    }

    private readonly struct BodyFrame
    {
        public readonly Vector2 Feet;
        public readonly float Height;

        public BodyFrame(Vector2 feet, float height)
        {
            Feet = feet;
            Height = height;
        }

        public Vector2 Point(Vector2 normalized)
        {
            return new Vector2(Feet.X + normalized.X * Height, Feet.Y - normalized.Y * Height);
        }

        public float ScanY(float reveal)
        {
            return Feet.Y - reveal * Height * 1.02f;
        }
    }
}
