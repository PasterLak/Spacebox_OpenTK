using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class GravityGeneratorUI
{
    private const string WindowId = "gravityGenerator";
    public const float PresetTolerance = 0.005f;

    private static GravityGeneratorBlock _block;
    private static string _blockName = "Gravity Generator";

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public static GravityGeneratorBlock Block => _block;
    public static string BlockName => _blockName;

    public static void Open(GravityGeneratorBlock block, LocalAstronaut player)
    {
        _block = block;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;
        UIManager.Open(WindowId);
    }

    public static void Update()
    {
        if (!IsOpen) return;
        if (Input.IsKeyDown(Keys.Escape) || Input.IsActionDown("inventory")) Close();
    }

    public static void Configure(int radius, float strength, bool enabled, bool insideOnly)
    {
        if (_block != null) Spacebox.Network.GravityConfigNetwork.Configure(_block, radius, strength, enabled, insideOnly);
    }

    public static void ApplyPendingPower()
    {
        _block?.ApplyPendingPower();
    }

    public static (string Text, Spacebox.Game.GUI.Canvas.Windows.StatusKind Kind) Status()
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

    private static void Close()
    {
        _block?.ApplyPendingPower();
        _block = null;
        UIManager.CloseAll();
    }

    public static void Reset()
    {
        _block = null;
    }
}
