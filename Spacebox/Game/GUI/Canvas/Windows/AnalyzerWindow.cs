using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Network;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class AnalyzerWindow : GameWindow
{
    private const float Width = 320f;
    private const float ContentHeight = 170f;
    private const float MineralIconSize = 18f;

    private enum View { NoPower, Scanning, Complete, Empty }

    private readonly Label _scanTime = new Label();
    private readonly Label _deviation = new Label();
    private readonly Label _message = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly VBoxContainer _scanning = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = BoxAlignment.Center };
    private readonly ProgressBar _progress = new ProgressBar { MaxValue = 1f, CustomMinimumSize = new Vector2(Width - 60f, 16f) };
    private readonly VBoxContainer _results = new VBoxContainer();
    private readonly Label _mass = new Label { ThemeTypeVariation = "SectionLabel" };
    private readonly VBoxContainer _minerals = new VBoxContainer();
    private readonly HBoxContainer _buttons = new HBoxContainer { Alignment = BoxAlignment.Center };
    private readonly Button _scan = new Button { Text = "Scan Asteroid", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Button _cancel = new Button { Text = "Cancel Scan", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Button _rescan = new Button { Text = "Rescan", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Button _clear = new Button { Text = "Clear Data", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private int _mineralsVersion = -1;
    private View _view = (View)(-1);

    public AnalyzerWindow() : base("Analyzer")
    {
        Name = "Analyzer";
        Body.AddChild(new Label { Text = "Scanning allows determining the approximate composition of the asteroid.", Autowrap = true, CustomMinimumSize = new Vector2(Width, 0f) });
        Body.AddChild(_scanTime);
        Body.AddChild(_deviation);
        Body.AddChild(new HSeparator());

        var content = Body.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(Width, ContentHeight) });
        content.AddChild(_message);
        content.AddChild(_scanning);
        _scanning.AddChild(new Label { Text = "Scanning in progress...", ThemeTypeVariation = "WarningLabel", HorizontalAlignment = HorizontalAlignment.Center });
        _scanning.AddChild(_progress);
        content.AddChild(_results);
        _results.SizeFlagsVertical = SizeFlags.ExpandFill;
        _results.AddChild(_mass);
        _results.AddChild(new HSeparator());
        _results.AddChild(new Label { Text = "Detected Minerals:" });
        var scroll = _results.AddChild(new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill });
        scroll.AddChild(_minerals);
        _minerals.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        Body.AddChild(_buttons);
        foreach (var button in new[] { _scan, _cancel, _rescan, _clear }) _buttons.AddChild(button);
        _scan.Pressed += _ => AnalyzerUI.Request(AnalyzerAction.Start);
        _rescan.Pressed += _ => AnalyzerUI.Request(AnalyzerAction.Start);
        _cancel.Pressed += _ => AnalyzerUI.Request(AnalyzerAction.Cancel);
        _clear.Pressed += _ => AnalyzerUI.Request(AnalyzerAction.Clear);
    }

    public override bool IsOpen => AnalyzerUI.IsOpen;

    protected override void Refresh()
    {
        var block = AnalyzerUI.Block;
        if (TitleLabel.Text != AnalyzerUI.BlockName) TitleLabel.Text = AnalyzerUI.BlockName;
        ShowEnergy(block.ConsumptionRate);
        SetText(_scanTime, $"Scan time: {AnalyzerUI.ScanSeconds:0.0} s.");
        SetText(_deviation, $"Deviation: {block.DeviationPercentage}%");

        var view = !AnalyzerUI.HasPower ? View.NoPower : block.IsScanning ? View.Scanning : block.ScanComplete ? View.Complete : View.Empty;
        if (view != _view) Show(view);

        if (view == View.Scanning) _progress.Value = block.Progress;
        if (view == View.Complete && block.CachedMinerals.Count + (int)(block.CachedMass % 100000) != _mineralsVersion) BuildMinerals();
    }

    private void Show(View view)
    {
        _view = view;
        _message.Visible = view == View.NoPower || view == View.Empty;
        StatusText.Set(_message, view == View.NoPower ? "NO POWER" : "Start scanning to collect data.", view == View.NoPower ? StatusKind.Bad : StatusKind.Normal);
        _scanning.Visible = view == View.Scanning;
        _results.Visible = view == View.Complete;
        _buttons.Visible = view != View.NoPower;
        _scan.Visible = view == View.Empty;
        _cancel.Visible = view == View.Scanning;
        _rescan.Visible = view == View.Complete;
        _clear.Visible = view == View.Complete;
        _mineralsVersion = -1;
    }

    private void BuildMinerals()
    {
        var block = AnalyzerUI.Block;
        _mineralsVersion = block.CachedMinerals.Count + (int)(block.CachedMass % 100000);
        _mass.Text = $"Estimated Mass: {block.CachedMass.ToString("N0").Replace(",", ".")} tn";
        _minerals.ClearChildren();
        if (block.CachedMinerals.Count == 0)
        {
            _minerals.AddChild(StatusText.Create("No minerals detected.", StatusKind.Bad));
            return;
        }

        string approx = block.DeviationPercentage == 0f ? "" : "~";
        var sorted = new List<KeyValuePair<short, int>>(block.CachedMinerals);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var pair in sorted)
        {
            var row = _minerals.AddChild(new HBoxContainer());
            row.AddThemeConstantOverride("separation", 6f);
            var icon = row.AddChild(new ItemIconControl(MineralIconSize));
            var data = GameAssets.GetBlockDataById(pair.Key);
            string name = data != null ? data.Name : $"Unknown ID: {pair.Key}";
            if (GameAssets.TryGetItemByBlockID(pair.Key, out Item item) && item != null) icon.Set(item.Icon, 0, name);
            row.AddChild(new Label { Text = name, ThemeTypeVariation = "SuccessLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
            row.AddChild(new Label { Text = approx + pair.Value, VerticalAlignment = VerticalAlignment.Center });
        }
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
