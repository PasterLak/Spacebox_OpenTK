using Engine;
using Engine.InputPro;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Waypoints;

public sealed class WaypointWindowUI : IGameUiController
{
    public const string WindowId = "waypoints";
    private const string ToggleAction = "waypoints";

    private Tag _selected;
    private bool _confirmDelete;
    private readonly ChatController _chat;

    public WaypointWindowUI(ChatController chat)
    {
        _chat = chat;
    }

    public bool IsOpen => UIManager.IsOpen(WindowId);
    public Tag Selected => _selected;
    public bool ConfirmDelete => _confirmDelete;

    public void Update()
    {
        if (_chat.FocusInput || Debug.IsVisible || GameScene.Current?.Tags == null) return;
        if (Spacebox.Game.GUI.Canvas.GameCanvas.WantsKeyboard) return;
        if (!InputManager.Instance.HasAction(ToggleAction) || !Input.IsActionDown(ToggleAction)) return;

        if (IsOpen)
        {
            if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
        }
        else if (!UIManager.IsUIMode && World.Instance?.Player?.IsAlive != false)
        {
            _confirmDelete = false;
            UIManager.Open(WindowId);
        }
    }

    public void Reset()
    {
        _selected = null;
        _confirmDelete = false;
    }

    public IReadOnlyList<Tag> Collect()
    {
        var waypoints = WaypointService.Collect();
        if (_selected != null && !WaypointService.Contains(_selected)) Select(null);
        return waypoints;
    }

    public void Select(Tag tag)
    {
        _selected = tag;
        _confirmDelete = false;
    }

    public void AddAtPlayer()
    {
        var player = World.Instance?.Player;
        if (player == null) return;
        Select(WaypointService.Add(player.Position));
        Spacebox.Game.GUI.Sounds.UiSounds.Play(Spacebox.Game.GUI.Sounds.UiSound.WaypointAdded);
    }

    public void Close()
    {
        if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
    }

    public void Rename(string name)
    {
        if (_selected != null) WaypointService.Rename(_selected, name);
    }

    public void SetColor(Color4 color)
    {
        if (_selected == null) return;
        WaypointService.SetColor(_selected, color);
        WaypointService.Save();
    }

    public void SetVisible(bool visible)
    {
        if (_selected == null) return;
        WaypointService.SetVisible(_selected, visible);
        WaypointService.Save();
    }

    public void Save() => WaypointService.Save();

    public void Delete()
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

    public string DistanceTo(Tag tag)
    {
        var player = World.Instance?.Player;
        return player == null || tag == null ? "" : FormatDistance(Vector3.Distance(player.Position, tag.WorldPosition));
    }

    public static string FormatDistance(float meters)
    {
        return meters >= 1000f ? $"{meters / 1000f:0.0} km" : $"{meters:0} m";
    }
}
