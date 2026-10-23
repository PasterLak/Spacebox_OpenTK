using Engine;
using Engine.InputPro;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Waypoints;

public static class WaypointWindowUI
{
    public const string WindowId = "waypoints";
    private const string ToggleAction = "waypoints";

    private static Tag _selected;
    private static bool _confirmDelete;

    public static bool IsOpen => UIManager.IsOpen(WindowId);
    public static Tag Selected => _selected;
    public static bool ConfirmDelete => _confirmDelete;

    public static void Update()
    {
        if (Chat.FocusInput || Debug.IsVisible || TagManager.Instance == null) return;
        if (Spacebox.Game.GUI.Canvas.GameCanvas.WantsKeyboard) return;
        if (!InputManager.Instance.HasAction(ToggleAction) || !Input.IsActionDown(ToggleAction)) return;

        if (IsOpen)
        {
            if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
        }
        else if (!UIManager.IsUIMode)
        {
            _confirmDelete = false;
            UIManager.Open(WindowId);
        }
    }

    public static void Reset()
    {
        _selected = null;
        _confirmDelete = false;
    }

    public static IReadOnlyList<Tag> Collect()
    {
        var waypoints = WaypointService.Collect();
        if (_selected != null && !WaypointService.Contains(_selected)) Select(null);
        return waypoints;
    }

    public static void Select(Tag tag)
    {
        _selected = tag;
        _confirmDelete = false;
    }

    public static void AddAtPlayer()
    {
        var player = World.Instance?.Player;
        if (player == null) return;
        Select(WaypointService.Add(player.Position));
    }

    public static void Close()
    {
        if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
    }

    public static void Rename(string name)
    {
        if (_selected != null) WaypointService.Rename(_selected, name);
    }

    public static void SetColor(Color4 color)
    {
        if (_selected == null) return;
        WaypointService.SetColor(_selected, color);
        WaypointService.Save();
    }

    public static void SetVisible(bool visible)
    {
        if (_selected == null) return;
        WaypointService.SetVisible(_selected, visible);
        WaypointService.Save();
    }

    public static void Save() => WaypointService.Save();

    public static void Delete()
    {
        if (_selected == null) return;
        if (!_confirmDelete)
        {
            _confirmDelete = true;
            return;
        }

        WaypointService.Remove(_selected);
        Select(null);
    }

    public static string DistanceTo(Tag tag)
    {
        var player = World.Instance?.Player;
        return player == null || tag == null ? "" : FormatDistance(Vector3.Distance(player.Position, tag.WorldPosition));
    }

    public static string FormatDistance(float meters)
    {
        return meters >= 1000f ? $"{meters / 1000f:0.0} km" : $"{meters:0} m";
    }
}
