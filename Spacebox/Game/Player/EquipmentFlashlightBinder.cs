namespace Spacebox.Game.Player;

public sealed class EquipmentFlashlightBinder
{
    private readonly LocalAstronaut _player;
    private EquipmentItem? _appliedItem;
    private bool _isApplied;

    public EquipmentFlashlightBinder(LocalAstronaut player)
    {
        _player = player;
        Apply();
    }

    public void Update()
    {
        var item = _player.ActiveFlashlightItem;
        if (!_isApplied || item != _appliedItem) Apply();
        else DisableIfUnavailable();
    }

    public void Apply()
    {
        var flashlight = _player.Flashlight;
        if (flashlight == null) return;

        _appliedItem = _player.ActiveFlashlightItem;
        _isApplied = true;
        flashlight.Apply(_appliedItem?.Flashlight ?? FlashlightSettings.Default);
        DisableIfUnavailable();
    }

    private void DisableIfUnavailable()
    {
        var flashlight = _player.Flashlight;
        if (flashlight.Enabled && !_player.CanUseFlashlight) flashlight.Enabled = false;
    }
}
