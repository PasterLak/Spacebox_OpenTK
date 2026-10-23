using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas;

public sealed class HudView : SafeArea
{
    public const float SlotSize = 43f;
    public const float BottomMargin = 26f;
    private const float BarGap = 10f;

    private readonly VBoxContainer _column = new VBoxContainer();
    private readonly HBoxContainer _bars = new HBoxContainer();
    private readonly HBoxContainer _hotbar = new HBoxContainer();
    private readonly StatBarControl _health = new StatBarControl(GameCanvasThemes.Health);
    private readonly StatBarControl _oxygen = new StatBarControl(GameCanvasThemes.Oxygen);
    private readonly StatBarControl _power = new StatBarControl(GameCanvasThemes.Power);
    private readonly List<HudSlotControl> _slots = new List<HudSlotControl>();
    private Storage _storage;

    public HudView()
    {
        Name = "Hud";
        MouseFilter = MouseFilter.Ignore;
        AddChild(_column);
        _column.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, BottomMargin);
        _column.GrowVertical = GrowDirection.Begin;
        _column.GrowHorizontal = GrowDirection.Both;
        _column.AddThemeConstantOverride("separation", 6f);

        _bars.AddThemeConstantOverride("separation", BarGap);
        foreach (var bar in new[] { _health, _oxygen, _power })
        {
            bar.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
            _bars.AddChild(bar);
        }

        _column.AddChild(_bars);
        _column.AddChild(_hotbar);
    }

    public void Refresh(LocalAstronaut player)
    {
        bool showHotbar = HudVisibility.ShowHotbar && PanelUI.Storage != null && GameCanvas.DrawsHotbar;
        bool showBars = HudVisibility.ShowPlayerHud && player != null;
        Present(showHotbar ? PanelUI.Storage : null, PanelUI.SelectedSlotId, showBars);
        if (!showBars) return;

        SetBar(_health, player.HealthBar.StatsGUI);
        SetBar(_oxygen, player.OxygenBar.StatsGUI);
        SetBar(_power, player.PowerBar.StatsGUI);
    }

    public void Present(Storage hotbar, int selectedSlot, bool showBars)
    {
        _hotbar.Visible = hotbar != null;
        _bars.Visible = showBars;
        _column.Visible = hotbar != null || showBars;
        if (hotbar != null) RefreshHotbar(hotbar, selectedSlot);
    }

    public void SetBars((int Value, int Max, string Text) health, (int Value, int Max, string Text) oxygen, (int Value, int Max, string Text) power)
    {
        _health.Set(health.Value, health.Max, health.Text);
        _oxygen.Set(oxygen.Value, oxygen.Max, oxygen.Text);
        _power.Set(power.Value, power.Max, power.Text);
    }

    private static void SetBar(StatBarControl bar, StatsGUI gui)
    {
        bar.Fill = new Color4(gui.FillColor.X, gui.FillColor.Y, gui.FillColor.Z, gui.FillColor.W);
        bar.Set(gui.StatsData.Value, gui.StatsData.MaxValue, gui.TextOverride);
    }

    private void RefreshHotbar(Storage storage, int selected)
    {
        if (!ReferenceEquals(storage, _storage)) Rebuild(storage);

        float spacing = GetThemeConstant("slot_spacing", 0f);
        _hotbar.AddThemeConstantOverride("separation", spacing);
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Selected = i == selected;
            _slots[i].Refresh();
        }
    }

    private void Rebuild(Storage storage)
    {
        _storage = storage;
        _hotbar.ClearChildren();
        _slots.Clear();
        for (int column = 0; column < storage.SizeY; column++)
        {
            var slot = new HudSlotControl { CustomMinimumSize = new Vector2(SlotSize, SlotSize) };
            slot.Bind(storage.GetSlot(0, column));
            slot.Clicked += PanelUI.OnSlotClicked;
            _slots.Add(slot);
            _hotbar.AddChild(slot);
        }
    }
}
