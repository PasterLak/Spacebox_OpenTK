using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI.Canvas;

public sealed class HudView : SafeArea
{
    public const float SlotSize = 43f;
    public const float BottomMargin = 26f;
    private const float BarGap = 10f;
    private const float MinSlotSize = 20f;
    private const float SideMargin = 24f;

    private readonly VBoxContainer _column = new VBoxContainer();
    private readonly HBoxContainer _bars = new HBoxContainer();
    private readonly VBoxContainer _hotbar = new VBoxContainer();
    private readonly PanelContainer _hotbarPanel = new PanelContainer { Name = "HotbarPanel", ThemeTypeVariation = "HotbarPanel", MouseFilter = MouseFilter.Ignore, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
    private readonly StatBarControl _health = new StatBarControl(GameCanvasThemes.Health);
    private readonly StatBarControl _oxygen = new StatBarControl(GameCanvasThemes.Oxygen);
    private readonly StatBarControl _power = new StatBarControl(GameCanvasThemes.Power);
    private readonly List<Items.ItemSlotControl> _slots = new List<Items.ItemSlotControl>();
    private readonly Hud.HitImageControl _hitImage = new Hud.HitImageControl();
    private readonly Hud.SelectedItemNameControl _itemName;
    private readonly PanelController _panel;
    private Storage _storage;
    private int _builtSlots = -1;
    private int _builtPerRow = -1;
    private float _slotSize = SlotSize;

    public HudView(GameUi ui)
    {
        _panel = ui.Panel;
        Name = "Hud";
        MouseFilter = MouseFilter.Ignore;
        AddOverlay(new Hud.CameraFeedControl());
        AddOverlay(new Hud.TagsOverlayControl());
        AddOverlay(new Hud.TutorialPanelControl(ui.Tutorial));
        AddOverlay(_hitImage);
        AddOverlay(new Hud.CrosshairControl());
        AddOverlay(new Hud.HitMarkerControl(ui.HitMarker));
        AddOverlay(new Hud.RepairTargetControl(ui.RepairTarget));
        AddOverlay(new Hud.ItemControlsControl(ui.ItemControls));
        AddOverlay(new Hud.GravityIndicatorControl(ui.GravityIndicator));
        AddOverlay(new Hud.ShipHudControl(ui.ShipConsole));
        AddOverlay(new Hud.FpsCounterControl());
        _itemName = new Hud.SelectedItemNameControl(ui.Panel);
        _itemName.Above = _column;
        AddOverlay(_itemName);
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
        _column.AddChild(_hotbarPanel);
        _hotbarPanel.AddChild(_hotbar);
    }

    private void AddOverlay(Control overlay)
    {
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);
    }

    public void Refresh(LocalAstronaut player)
    {
        _hitImage.Image = player?.HitImage;
        bool showHotbar = HudVisibility.ShowHotbar && _panel.Storage != null && GameCanvas.DrawsHotbar;
        bool showBars = HudVisibility.ShowPlayerHud && player != null;
        Present(showHotbar ? _panel.Storage : null, _panel.SelectedSlotId, showBars, _panel.ActiveSlots, _panel.SlotsPerRow);
        if (!showBars) return;

        SetBar(_health, player.HealthBar.StatsGUI);
        SetBar(_oxygen, player.OxygenBar.StatsGUI);
        SetBar(_power, player.PowerBar.StatsGUI);
    }

    public void Present(Storage hotbar, int selectedSlot, bool showBars, int activeSlots = Player.GameModes.HotbarLayout.DefaultSlotsPerRow, int slotsPerRow = Player.GameModes.HotbarLayout.DefaultSlotsPerRow)
    {
        _hotbarPanel.Visible = hotbar != null;
        _bars.Visible = showBars;
        _column.Visible = hotbar != null || showBars;
        if (hotbar != null) RefreshHotbar(hotbar, selectedSlot, Math.Min(activeSlots, hotbar.SizeY), Math.Max(1, slotsPerRow));
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

    private void RefreshHotbar(Storage storage, int selected, int activeSlots, int slotsPerRow)
    {
        if (!ReferenceEquals(storage, _storage) || activeSlots != _builtSlots || slotsPerRow != _builtPerRow) Rebuild(storage, activeSlots, slotsPerRow);

        float spacing = GetThemeConstant("slot_spacing", 0f);
        _hotbar.AddThemeConstantOverride("separation", spacing);
        for (int i = 0; i < _hotbar.Children.Count; i++)
            if (_hotbar.Children[i] is HBoxContainer row) row.AddThemeConstantOverride("separation", spacing);

        ApplySlotSize();
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Selected = i == selected;
            _slots[i].Refresh();
        }
    }

    public static float FitSlotSize(float availableWidth, int slotsPerRow, float spacing)
    {
        if (availableWidth <= 0f || slotsPerRow <= 0) return SlotSize;
        float fit = MathF.Floor((availableWidth - spacing * (slotsPerRow - 1)) / slotsPerRow);
        return Math.Clamp(fit, MinSlotSize, SlotSize);
    }

    protected override void LayoutChildren()
    {
        ApplySlotSize();
        base.LayoutChildren();
    }

    private void ApplySlotSize()
    {
        if (_builtPerRow <= 0) return;

        float spacing = GetThemeConstant("slot_spacing", 0f);
        float size = FitSlotSize(Size.X - SideMargin * 2f - 24f, _builtPerRow, spacing);
        if (size == _slotSize) return;

        _slotSize = size;
        var minimum = new Vector2(size, size);
        for (int i = 0; i < _slots.Count; i++) _slots[i].CustomMinimumSize = minimum;
    }

    private void Rebuild(Storage storage, int activeSlots, int slotsPerRow)
    {
        _storage = storage;
        _builtSlots = activeSlots;
        _builtPerRow = slotsPerRow;
        _hotbar.ClearChildren();
        _slots.Clear();

        int rows = (activeSlots + slotsPerRow - 1) / slotsPerRow;
        var rowBoxes = new HBoxContainer[rows];
        for (int r = rows - 1; r >= 0; r--)
        {
            rowBoxes[r] = new HBoxContainer { MouseFilter = MouseFilter.Ignore, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            _hotbar.AddChild(rowBoxes[r]);
        }

        for (int index = 0; index < activeSlots; index++)
        {
            var slot = new Items.ItemSlotControl(_slotSize);
            slot.Bind(storage.GetSlot(0, index));
            slot.Clicked += _panel.OnSlotClicked;
            _slots.Add(slot);
            rowBoxes[index / slotsPerRow].AddChild(slot);
        }
    }
}
