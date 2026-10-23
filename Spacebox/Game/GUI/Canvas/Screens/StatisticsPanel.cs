using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class StatisticsPanel : PanelContainer
{
    private const float Width = 330f;
    private const float ListHeight = 320f;
    private const float RefreshInterval = 1f;

    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private PlayerStatistics _shown;
    private float _timer;

    private readonly PauseUI _pause;
    private readonly StatisticsUI _statisticsUi;

    public StatisticsPanel(PauseUI pause, StatisticsUI statisticsUi)
    {
        _pause = pause;
        _statisticsUi = statisticsUi;
        Name = "Statistics";
        Visible = false;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var frame = MenuLayout.Window("STATISTICS", Width);
        AddThemeStyleBoxOverride("panel", new StyleBoxEmpty());
        AddChild(frame.Root);
        _rows.AddThemeConstantOverride("separation", 3f);
        frame.Body.AddChild(MenuLayout.ScrollList(_rows, ListHeight));
        frame.Footer.AddChild(MenuLayout.Button(null, "Close", _pause.ToggleStatistics, false));
        frame.Footer.AddChild(MenuLayout.Spacer());
    }

    public void Sync(float deltaTime)
    {
        bool shown = _statisticsUi.IsVisible;
        if (Visible != shown) Visible = shown;
        if (!shown)
        {
            _shown = null;
            return;
        }

        _timer -= deltaTime;
        if (ReferenceEquals(_shown, _statisticsUi.Statistics) && _timer > 0f) return;
        _shown = _statisticsUi.Statistics;
        _timer = RefreshInterval;
        Show(StatisticsRows.Build(_shown, GameTime.ToString()));
    }

    public void Show(List<StatisticsRow> rows)
    {
        _rows.ClearChildren();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.IsHeader)
            {
                //float centerX = (width - textSize.X) * 0.5f;
                if (i > 0) _rows.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f), MouseFilter = MouseFilter.Ignore });
                _rows.AddChild(new Label { Text = row.Label, ThemeTypeVariation = "WarningLabel", Bold = true });
                _rows.AddChild(new HSeparator());
                continue;
            }

            var line = _rows.AddChild(new HBoxContainer());
            line.AddChild(new Label { Text = row.Label, SizeFlagsHorizontal = SizeFlags.ExpandFill });
            line.AddChild(new Label { Text = row.Value });
        }
    }
}
