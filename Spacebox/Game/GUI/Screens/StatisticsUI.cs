using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class StatisticsUI
{
    private static bool _isVisible = false;
    private static PlayerStatistics _statistics;

    public static bool IsVisible
    {
        get => _isVisible && _statistics != null;
        set => _isVisible = value;
    }

    public static PlayerStatistics Statistics => _statistics;

    public static void Show(PlayerStatistics statistics)
    {
        _statistics = statistics;
        _isVisible = true;
    }

    public static void Hide()
    {
        _isVisible = false;
        _statistics = null;
    }
}
