using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class MachineSoundSource : IDisposable
{
    private IDisposable? _finishedWatch;

    public MachineSoundSource(Block block, SpaceEntity entity, Vector3 local, IMachineActivity activity, string workingSound, string finishedSound)
    {
        Block = block;
        Entity = entity;
        Local = local;
        Activity = activity;
        WorkingSound = workingSound ?? "";
        FinishedSound = finishedSound ?? "";
        if (FinishedSound.Length > 0) _finishedWatch = activity.WatchFinished(block, PlayFinished);
    }

    public Block Block { get; }
    public SpaceEntity Entity { get; }
    public Vector3 Local { get; }
    public IMachineActivity Activity { get; }
    public string WorkingSound { get; }
    public string FinishedSound { get; }
    public float DistanceSquared { get; set; }

    public bool HasLoop => WorkingSound.Length > 0;
    public bool IsValid => Entity != null && !Entity.IsDisposed;
    public bool IsWorking => IsValid && Activity.IsWorking(Block);
    public Vector3 WorldPosition => Entity.EntityLocalToWorld(Local);

    private void PlayFinished()
    {
        if (IsValid) BlockEventSounds.Play(FinishedSound, WorldPosition);
    }

    public void Dispose()
    {
        _finishedWatch?.Dispose();
        _finishedWatch = null;
    }
}
