using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public sealed class RemoteDrillDust
{
    private const float SurfaceOffset = 0.05f;

    private readonly Astronaut _owner;
    private readonly Ray _ray = new Ray(Vector3.Zero, -Vector3.UnitZ, 1f);
    private BlockMiningEffect _effect;

    public RemoteDrillDust(Astronaut owner)
    {
        _owner = owner;
    }

    public void Update(bool isUsing, Item heldItem)
    {
        var sector = World.CurrentSector;
        if (!isUsing || heldItem is not DrillItem drill || sector == null || Application.IsHeadless || !Settings.Graphics.EffectsEnabled)
        {
            Hide();
            return;
        }

        _ray.Origin = _owner.Position;
        _ray.Direction = _owner.Front;
        _ray.Length = drill.Range;

        if (!sector.Raycast(_ray, out var hit) || hit.block == null)
        {
            Hide();
            return;
        }

        _effect ??= _owner.AddChild(BlockMiningEffect.CreateDefault());

        var position = hit.hitPosition + hit.normalWorld * SurfaceOffset;
        _effect.Enabled = true;
        _effect.ParticleSystem.Position = position;
        if (_effect.ParticleSystem.Emitter is ConeEmitter cone)
            cone.Direction = Vector3.Normalize(_owner.Position - position);
    }

    public void Dispose()
    {
        if (_effect == null) return;

        _owner.RemoveChild(_effect);
        _effect.Destroy();
        _effect = null;
    }

    private void Hide()
    {
        if (_effect == null || !_effect.Enabled) return;

        _effect.ClearParticles();
        _effect.Enabled = false;
    }
}
