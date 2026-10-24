using Spacebox.Game.Effects.Machines;
using Spacebox.Game.GUI;
using Spacebox.Game.Pictures;
using Spacebox.Game.Player;
using Spacebox.Game.Protection;
using Spacebox.Game.Rules;
using Spacebox.Game.Ships;
using Spacebox.Network.Players;

namespace Spacebox.Game;

public sealed class GameScene : IDisposable
{
    private bool _disposed;

    public static GameScene Current { get; private set; }

    public GameScene()
    {
        Creative = new CreativeTools();
        Rider = new ShipRider();
        Binding = new ShipBindingTool();
        Pilot = new LocalShipPilot(Rider, Binding);
        Skins = new PlayerSkinStore();
        WorldRules = new WorldRulesStore();
        Sprint = new SprintAssist();
        Claims = new ClaimVisuals();
        PictureFrames = new PictureFrameInteraction();
        MachineSounds = new MachineSounds();
    }

    public CreativeTools Creative { get; }
    public ShipRider Rider { get; }
    public ShipBindingTool Binding { get; }
    public LocalShipPilot Pilot { get; }
    public PlayerSkinStore Skins { get; }
    public WorldRulesStore WorldRules { get; }
    public SprintAssist Sprint { get; }
    public ClaimVisuals Claims { get; }
    public PictureFrameInteraction PictureFrames { get; }
    public MachineSounds MachineSounds { get; }
    public TagManager Tags { get; private set; }

    public bool IsDisposed => _disposed;

    public void MakeCurrent()
    {
        Current = this;
    }

    public void AttachTags(TagManager tags)
    {
        Tags = tags;
    }

    public void DetachTags(TagManager tags)
    {
        if (Tags == tags) Tags = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Pilot.Reset();
        Rider.Reset();
        MachineSounds.Clear();
        Tags = null;
        if (Current == this) Current = null;
    }
}
