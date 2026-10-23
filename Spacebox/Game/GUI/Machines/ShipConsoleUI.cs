using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Ships;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ShipConsoleUI
{
    private const string WindowId = "shipConsole";

    private static readonly int[] _groupCounts = new int[ConsoleBindings.GroupColors.Length];
    private static int _waitingGroup = -1;
    private static bool _skipOpenKey;

    public static bool IsOpen => UIManager.IsOpen(WindowId);
    public static int WaitingGroup => _waitingGroup;
    public static ShipConsoleBlock Console => ShipPilot.Console;
    public static ConsoleBindings Bindings => Console?.Entity?.ShipControl.GetBindings(Console.LocalCell);

    public static string HudText
    {
        get
        {
            var console = ShipPilot.Console;
            if (console?.Entity == null || UIManager.IsUIMode) return null;

            float speed = console.Entity.Ship?.Motion.Velocity.Length ?? 0f;
            return ShipBindingTool.IsActive
                ? $"BINDING  {ShipBindingTool.DescribeHovered() ?? "Look at a ship block"}\nLMB / wheel: change group   RMB: back to seat"
                : $"Speed {speed:0.0} m/s\nInventory key: console   Use: leave";
        }
    }

    public static void HandleInput()
    {
        if (!ShipPilot.IsPiloting || Debug.IsVisible || Chat.FocusInput) return;
        if (UIManager.IsUIMode || !Input.IsActionDown("inventory")) return;

        ShipBindingTool.Exit();
        _waitingGroup = -1;
        _skipOpenKey = true;
        UIManager.Open(WindowId);
    }

    public static void Update()
    {
        if (!IsOpen) return;

        var console = ShipPilot.Console;
        var entity = console?.Entity;
        var bindings = entity?.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null)
        {
            Close();
            return;
        }

        if (_waitingGroup >= 0)
        {
            if (TryReadKey(out int key))
            {
                ShipNetwork.RequestSetKey(entity, console.LocalCell, _waitingGroup, key);
                ShipKeyMemory.Remember(console, bindings, _waitingGroup, key);
                _waitingGroup = -1;
            }
        }
        else if (_skipOpenKey)
        {
            _skipOpenKey = false;
        }
        else if (Input.IsKeyDown(Keys.Escape) || Input.IsActionDown("inventory"))
        {
            Close();
        }
    }

    public static int[] CountGroups(ConsoleBindings bindings)
    {
        Array.Clear(_groupCounts);
        foreach (var pair in bindings.Bound)
            if (pair.Value < _groupCounts.Length) _groupCounts[pair.Value]++;
        return _groupCounts;
    }

    public static void ToggleWaiting(int group)
    {
        _waitingGroup = _waitingGroup == group ? -1 : group;
    }

    public static void BindBlocks()
    {
        Close();
        ShipBindingTool.Enter();
    }

    public static void LeaveSeat()
    {
        Close();
        ShipPilot.Unmount(InventoryUI.Player);
    }

    public static void Reset()
    {
        _waitingGroup = -1;
        _skipOpenKey = false;
    }

    private static bool TryReadKey(out int key)
    {
        key = ShipKeys.None;
        if (Input.IsKeyDown(Keys.Escape) || Input.IsKeyDown(Keys.Backspace) || Input.IsKeyDown(Keys.Delete))
            return true;

        if (Input.IsMouseButtonDown(MouseButton.Left) && !Spacebox.Game.GUI.Canvas.GameCanvas.IsPointerOverInteractive)
        {
            key = ShipKeys.MouseLeft;
            return true;
        }

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            key = ShipKeys.MouseRight;
            return true;
        }

        var keyboard = Input.Keyboard;
        foreach (Keys candidate in KeyValues)
        {
            if (!keyboard.IsKeyPressed(candidate)) continue;
            key = (int)candidate;
            return true;
        }

        return false;
    }

    private static readonly Keys[] KeyValues = BuildKeyValues();

    private static Keys[] BuildKeyValues()
    {
        var values = (Keys[])Enum.GetValues(typeof(Keys));
        var result = new List<Keys>(values.Length);
        for (int i = 0; i < values.Length; i++)
            if (values[i] > 0 && !result.Contains(values[i])) result.Add(values[i]);
        return result.ToArray();
    }

    private static void Close()
    {
        _waitingGroup = -1;
        UIManager.CloseAll();
    }
}
