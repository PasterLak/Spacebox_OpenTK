using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class GeneratorWindow : GameWindow
{
    private static readonly Color4 Red = new Color4(1f, 0.2f, 0.2f, 1f);
    private static readonly Color4 Orange = new Color4(1f, 0.5f, 0f, 1f);
    private static readonly Color4 Yellow = new Color4(1f, 0.85f, 0.1f, 1f);
    private static readonly Color4 Green = new Color4(0.2f, 0.85f, 0.3f, 1f);

    private readonly Label _generating = new Label();
    private readonly Label _power = new Label();
    private readonly Label _consumption = new Label();
    private readonly ProgressBar _free = new ProgressBar { MaxValue = 1f, CustomMinimumSize = new Vector2(220f, 10f), ShowPercentage = false, TooltipText = "Free power of the network" };

    public GeneratorWindow() : base("Generator")
    {
        Name = "Generator";
        Body.AddChild(_generating);
        Body.AddChild(new HSeparator());
        Body.AddChild(new Label { Text = "Network", ThemeTypeVariation = "SectionLabel" });
        Body.AddChild(_power);
        Body.AddChild(_consumption);
        Body.AddChild(_free);
    }

    public override bool IsOpen => GeneratorUI.IsOpen;

    protected override void Refresh()
    {
        if (TitleLabel.Text != GeneratorUI.BlockName) TitleLabel.Text = GeneratorUI.BlockName;
        SetText(_generating, $"Generating: +{GeneratorUI.Block.GenerationRate} EU");
        SetText(_power, $"Power: {GeneratorUI.NetworkPower} EU");
        SetText(_consumption, $"Consumption: {GeneratorUI.NetworkConsumption} EU");
        _free.Value = GeneratorUI.FreeShare;
        _free.FillColor = ColorFor(GeneratorUI.RemainingPercent, GeneratorUI.NetworkPower);
    }

    private static Color4 ColorFor(int remaining, int power)
    {
        if (power == 0) return Color4.White;
        if (remaining < 33) return Red;
        if (remaining < 50) return Orange;
        if (remaining < 70) return Yellow;
        return Green;
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
