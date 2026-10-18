using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Network.Players;

public sealed class AstronautNetwork : NetworkTransform
{
    private const short NoItem = -1;

    private readonly NetworkVariable<bool> _flashlight;
    private readonly NetworkVariable<short> _heldItem;
    private readonly NetworkVariable<bool> _isAlive;
    private readonly NetworkVariable<bool> _isVulnerable;

    public static AstronautNetwork Local { get; private set; }

    public string PlayerName { get; set; }
    public bool IsAlive => _isAlive.Value;
    public bool IsVulnerable => _isAlive.Value && _isVulnerable.Value;

    public AstronautNetwork()
    {
        _flashlight = CreateVariable(false, NetworkWritePermission.Owner);
        _heldItem = CreateVariable(NoItem, NetworkWritePermission.Owner);
        _isAlive = CreateVariable(true, NetworkWritePermission.Owner);
        _isVulnerable = CreateVariable(false, NetworkWritePermission.Owner);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (IsSpawned && IsOwner && Owner is LocalAstronaut local)
            _isVulnerable.Value = local.GameMode == GameMode.Survival;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Owner is not Astronaut astronaut) return;

        if (IsOwner)
        {
            Local = this;
            _flashlight.Value = astronaut.Flashlight.Enabled;
            if (astronaut is LocalAstronaut localAstronaut)
            {
                _isAlive.Value = localAstronaut.IsAlive;
                localAstronaut.OnDeath += OnLocalDeath;
                localAstronaut.OnRevived += OnLocalRevived;
            }
            SetHeldItem(Spacebox.Game.GUI.PanelUI.CurrentSlot());
            astronaut.Flashlight.OnEnabledChanged += OnLocalFlashlightChanged;
            return;
        }

        _flashlight.Changed += (_, value) => astronaut.Flashlight.Enabled = value;
        _heldItem.Changed += (_, value) => astronaut.ChangeItemInHand(GetItem(value));
        _isAlive.Changed += (_, value) => ApplyAliveVisual(astronaut, value);
        ApplyAliveVisual(astronaut, _isAlive.Value);
        astronaut.Flashlight.Enabled = _flashlight.Value;
        astronaut.ChangeItemInHand(GetItem(_heldItem.Value));
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this)
        {
            Local = null;
            if (Owner is Astronaut astronaut) astronaut.Flashlight.OnEnabledChanged -= OnLocalFlashlightChanged;
            if (Owner is LocalAstronaut localAstronaut)
            {
                localAstronaut.OnDeath -= OnLocalDeath;
                localAstronaut.OnRevived -= OnLocalRevived;
            }
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

    private void OnLocalDeath()
    {
        _isAlive.Value = false;

        int killer = Owner is LocalAstronaut local ? local.GetRecentAttacker() : -1;
        SendRpc(nameof(ReportDeath), killer);
    }

    private void OnLocalRevived()
    {
        _isAlive.Value = true;
    }

    [Rpc(RpcTarget.Server)]
    private void ReportDeath(int killerPeer)
    {
        string victim = Spacebox.Network.Combat.CombatNetwork.GetPlayerName(OwnerPeer);
        string text = killerPeer > 0 && killerPeer != OwnerPeer
            ? $"{victim} was killed by {Spacebox.Network.Combat.CombatNetwork.GetPlayerName(killerPeer)}"
            : $"{victim} died";

        ChatService.Broadcast(text, new Color4(1f, 0.35f, 0.35f, 1f));
    }

    private static void ApplyAliveVisual(Astronaut astronaut, bool isAlive)
    {
        if (astronaut is RemoteAstronaut remote) remote.SetAliveVisual(isAlive);
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
