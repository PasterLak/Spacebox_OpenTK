using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;

namespace Spacebox.Network.Players;

public sealed class AstronautNetwork : NetworkTransform
{
    private const short NoItem = -1;

    private readonly NetworkVariable<bool> _flashlight;
    private readonly NetworkVariable<short> _heldItem;

    public static AstronautNetwork Local { get; private set; }

    public string PlayerName { get; set; }

    public AstronautNetwork()
    {
        _flashlight = CreateVariable(false, NetworkWritePermission.Owner);
        _heldItem = CreateVariable(NoItem, NetworkWritePermission.Owner);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Owner is not Astronaut astronaut) return;

        if (IsOwner)
        {
            Local = this;
            _flashlight.Value = astronaut.Flashlight.Enabled;
            SetHeldItem(Spacebox.Game.GUI.PanelUI.CurrentSlot());
            astronaut.Flashlight.OnEnabledChanged += OnLocalFlashlightChanged;
            return;
        }

        _flashlight.Changed += (_, value) => astronaut.Flashlight.Enabled = value;
        _heldItem.Changed += (_, value) => astronaut.ChangeItemInHand(GetItem(value));
        astronaut.Flashlight.Enabled = _flashlight.Value;
        astronaut.ChangeItemInHand(GetItem(_heldItem.Value));
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this)
        {
            Local = null;
            if (Owner is Astronaut astronaut) astronaut.Flashlight.OnEnabledChanged -= OnLocalFlashlightChanged;
        }
    }

    public void SetHeldItem(ItemSlot slot)
    {
        if (IsOwner) _heldItem.Value = slot != null && slot.HasItem ? slot.Item.Id : NoItem;
    }

    protected override Quaternion ReadRotation()
    {
        return Owner is Camera360Base camera ? camera.GetRotation() : base.ReadRotation();
    }

    protected override void ApplyRotation(Quaternion rotation)
    {
        if (Owner is Camera360Base camera) camera.SetRotation(rotation);
        if (Owner is RemoteAstronaut) base.ApplyRotation(rotation);
    }

    private void OnLocalFlashlightChanged(bool enabled)
    {
        _flashlight.Value = enabled;
    }

    private static Item GetItem(short id)
    {
        return id != NoItem && GameAssets.Items.TryGetValue(id, out var item) ? item : null;
    }
}
