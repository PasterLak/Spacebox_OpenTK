namespace Spacebox.Game.GUI;

public static class HitMarkerUI
{
    private const float Duration = 0.2f;

    private static float _timeLeft;

    public static bool IsVisible => _timeLeft > 0f;
    public static bool IsHeadshot { get; private set; }

    public static void Show(bool headshot)
    {
        _timeLeft = Duration;
        IsHeadshot = headshot;
    }

    public static void Update(float deltaTime)
    {
        if (_timeLeft > 0f) _timeLeft -= deltaTime;
    }

    public static void Reset()
    {
        _timeLeft = 0f;
    }
}
