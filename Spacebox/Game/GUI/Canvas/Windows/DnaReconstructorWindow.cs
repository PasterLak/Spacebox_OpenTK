using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Canvas.Items;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class DnaReconstructorWindow : GameWindow
{
    private const float Width = 300f;

    private readonly ItemSlotControl _slot = new ItemSlotControl();
    private readonly Label _samples = new Label();
    private readonly Label _power = new Label();
    private readonly Label _status = StatusText.Create();
    private DnaReconstructorBlock _shown;

    public DnaReconstructorWindow() : base("DNA Reconstructor")
    {
        Name = "DnaReconstructor";
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 10f);
        row.AddChild(_slot);
        var info = row.AddChild(new VBoxContainer());
        info.AddChild(_samples);
        info.AddChild(_power);
        info.AddChild(_status);
        Body.AddChild(new Label
        {
            Text = "After death you can be reconstructed at the nearest powered reconstructor. Uses 1 DNA sample.",
            ThemeTypeVariation = "MutedLabel",
            Autowrap = true,
            CustomMinimumSize = new Vector2(Width, 0f)
        });
        _slot.Clicked += DnaReconstructorUI.OnSlotClicked;
    }

    public override bool IsOpen => DnaReconstructorUI.IsOpen;

    protected override void Refresh()
    {
        var block = DnaReconstructorUI.Block;
        if (TitleLabel.Text != DnaReconstructorUI.BlockName) TitleLabel.Text = DnaReconstructorUI.BlockName;
        if (!ReferenceEquals(block, _shown))
        {
            _shown = block;
            _slot.Bind(DnaReconstructorUI.DnaSlot, DnaReconstructorUI.SlotSprite);
        }

        _slot.Refresh();
        SetText(_samples, $"DNA samples: {block.DnaCount}/{block.DnaCapacity}");
        SetText(_power, $"Power: {block.Data.Power} EU");
        var (text, kind) = DnaReconstructorUI.Status();
        StatusText.Set(_status, text, kind);
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
