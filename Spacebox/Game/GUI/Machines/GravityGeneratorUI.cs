using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public sealed class GravityGeneratorUI : IGameUiController
{
    private const string WindowId = "gravityGenerator";
    public const float PresetTolerance = 0.005f;

    private GravityGeneratorBlock _block;
    private string _blockName = "Gravity Generator";

    public bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public GravityGeneratorBlock Block => _block;
    public string BlockName => _blockName;

    public void Open(GravityGeneratorBlock block, LocalAstronaut player)
    {
        _block = block;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;
        UIManager.Open(WindowId);
    }

    public void OnGUI()
    {
        if (!IsOpen) return;
        if (Input.IsKeyDown(Keys.Escape) || Input.IsActionDown("inventory")) Close();
    }

    public void Configure(int radius, float strength, bool enabled, bool insideOnly)
    {
        if (_block != null) Spacebox.Network.GravityConfigNetwork.Configure(_block, radius, strength, enabled, insideOnly);
    }

    public void ApplyPendingPower()
    {
        _block?.ApplyPendingPower();
    }

    public (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind) Status()
    {
        if (_block == null) return ("", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Muted);
        if (!_block.IsGravityEnabled) return ("Off", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Muted);
        if (!_block.IsActive) return ("NO POWER", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Bad);
        if (!_block.InsideOnly) return ("Working", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Good);

        _block.UpdateRoom();
        return _block.Room.IsSealed
            ? ($"Working (room: {_block.Room.CellCount} blocks)", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Good)
            : ("Room is not closed", Spacebox.Game.GUI.Canvas.Windows.StatusKind.Warning);
    }

    private void Close()
    {
        _block?.ApplyPendingPower();
        _block = null;
        UIManager.CloseAll();
    }

    public void Reset()
    {
        _block = null;
    }
}
