using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public sealed class StatisticsUI : IGameUiController
{
    private bool _isVisible = false;
    private PlayerStatistics _statistics;

    public bool IsVisible
    {
        get => _isVisible && _statistics != null;
        set => _isVisible = value;
    }

    public PlayerStatistics Statistics => _statistics;

    public void Show(PlayerStatistics statistics)
    {
        _statistics = statistics;
        _isVisible = true;
    }

    public void Hide()
    {
        _isVisible = false;
        _statistics = null;
    }

    public void Reset() => Hide();
}
