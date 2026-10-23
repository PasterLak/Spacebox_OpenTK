using Engine;
using Spacebox.Game.GUI.Sounds;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class GameWindowSounds
{
    public const float NoPowerDelay = 0.18f;

    private GameWindow _opened;
    private GameWindow _closed;
    private GameWindow _noPowerWindow;
    private float _noPowerAt;

    public void Track(GameWindow window, bool wasVisible)
    {
        bool visible = window.Visible;
        if (visible == wasVisible) return;

        if (visible)
        {
            if (_opened == null || window.SoundPriority > _opened.SoundPriority) _opened = window;
        }
        else if (_closed == null || window.SoundPriority > _closed.SoundPriority)
        {
            _closed = window;
        }
    }

    public void Flush()
    {
        if (_opened != null)
        {
            UiSounds.Play(_opened.OpenSound);
            _noPowerWindow = _opened;
            _noPowerAt = Time.Total + NoPowerDelay;
        }
        else if (_closed != null)
        {
            UiSounds.Play(_closed.CloseSound);
        }

        _opened = null;
        _closed = null;
        PlayPendingNoPower();
    }

    private void PlayPendingNoPower()
    {
        if (_noPowerWindow == null) return;

        if (!_noPowerWindow.Visible)
        {
            _noPowerWindow = null;
            return;
        }

        float now = Time.Total;
        if (now < _noPowerAt && _noPowerAt - now <= NoPowerDelay) return;

        if (_noPowerWindow.LacksPower) UiSounds.Play(UiSound.NoPower);
        _noPowerWindow = null;
    }
}
