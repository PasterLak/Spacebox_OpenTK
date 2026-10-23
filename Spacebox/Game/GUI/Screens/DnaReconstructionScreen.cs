using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Player.Respawn;

namespace Spacebox.Game.GUI;

public sealed class DnaReconstructionScreen : IGameUiController
{
    public const float SkeletonEnd = 0.35f;
    public const float MuscleEnd = 0.62f;
    public const float SkinEnd = 0.88f;
    public const float FadeInEnd = 0.06f;
    private const float FlashFadeDuration = 0.8f;
    public const float BoneThickness = 0.012f;
    public const int HelixSegments = 48;
    public const int GridLines = 24;

    public static readonly Vector2[] Joints =
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

    public static readonly (int A, int B, float Muscle)[] Bones =
    {
        (1, 2, 2.2f), (2, 3, 4.5f), (3, 4, 4f),
        (2, 5, 2.5f), (2, 6, 2.5f),
        (5, 7, 2.6f), (6, 8, 2.6f), (7, 9, 2.1f), (8, 10, 2.1f), (9, 11, 1.6f), (10, 12, 1.6f),
        (4, 13, 3f), (4, 14, 3f), (13, 15, 3.2f), (14, 16, 3.2f), (15, 17, 2.4f), (16, 18, 2.4f), (17, 19, 1.5f), (18, 20, 1.5f)
    };

    public static readonly float[] RibHeights = { 0.8f, 0.76f, 0.72f, 0.68f };

    public static readonly (float Start, string Text)[] Steps =
    {
        (0f, "SEQUENCING DNA"),
        (FadeInEnd, "RECONSTRUCTING SKELETAL STRUCTURE"),
        (SkeletonEnd, "GROWING MUSCLE TISSUE"),
        (MuscleEnd, "RESTORING NERVOUS SYSTEM AND SKIN"),
        (SkinEnd, "RECONSTRUCTION COMPLETE")
    };

    private float _duration = 5f;
    private float _elapsed;
    private Action _onComplete;
    private IReconstructionEffects _effects;

    public bool IsActive { get; private set; }
    public float Elapsed => _elapsed;
    public float Progress => Math.Clamp(_elapsed / _duration, 0f, 1f);

    public void Play(float duration, Action onComplete, IReconstructionEffects effects = null)
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

    public void Tick()
    {
        if (!IsActive) return;

        _elapsed += Time.Delta;
        float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
        _effects?.Update(t, StepIndex(t));
        if (_elapsed < _duration) return;

        var onComplete = _onComplete;
        Reset();
        onComplete?.Invoke();
        ColorOverlay.FadeOut(System.Numerics.Vector3.One, 1f, FlashFadeDuration);
    }

    public void Reset()
    {
        _effects?.End();
        _effects = null;
        IsActive = false;
        _onComplete = null;
        _elapsed = 0f;
    }

    public static int StepIndex(float t)
    {
        int index = 0;
        for (int i = 0; i < Steps.Length; i++)
        {
            if (t >= Steps[i].Start) index = i;
        }
        return index;
    }

    public static float Phase(float t, float start, float end)
    {
        return Math.Clamp((t - start) / (end - start), 0f, 1f);
    }
}
