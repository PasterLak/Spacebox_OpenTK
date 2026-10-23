using Engine;

namespace Spacebox.Game.GUI;

public sealed class HitMarkerUI : IGameUiController
{
    private const float Duration = 0.2f;

    private float _timeLeft;

    public bool IsVisible => _timeLeft > 0f;
    public bool IsHeadshot { get; private set; }

    public void Show(bool headshot)
    {
        _timeLeft = Duration;
        IsHeadshot = headshot;
    }

    void IGameUiController.Update() => Update(Time.Delta);

    public void Update(float deltaTime)
    {
        if (_timeLeft > 0f) _timeLeft -= deltaTime;
    }

    public void Reset()
    {
        _timeLeft = 0f;
    }
}
