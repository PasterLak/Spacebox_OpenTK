using Engine;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Player.Interactions;

public class InteractionHandler
{
    private InteractionMode _interaction;
    public InteractionMode Interaction
    {
        get => _interaction;
        set { SetInteraction(value, _gameMode); }
    }
    private readonly HashSet<Type> _allowedInteractions;
    private readonly List<IPassiveInteraction> _passiveInteractions = new List<IPassiveInteraction>();

    private readonly GameMode _gameMode;
    public InteractionHandler(GameMode gameMode)
    {
        _gameMode = gameMode;
        _allowedInteractions = new HashSet<Type>()
        {
            typeof(InteractionDefault)
        };

        SetInteraction(new InteractionDefault(), gameMode);
    }

    public InteractionHandler(InteractionMode defaultMode, HashSet<Type> allowedInteractions, GameMode gameMode)
    {
        _gameMode = gameMode;
        _allowedInteractions = allowedInteractions;

        _allowedInteractions.Add(typeof(InteractionDefault));
        SetInteraction(defaultMode, gameMode);
    }

    public InteractionHandler(InteractionMode defaultMode, HashSet<Type> allowedInteractions, GameMode gameMode, IEnumerable<IPassiveInteraction> passiveInteractions)
        : this(defaultMode, allowedInteractions, gameMode)
    {
        _passiveInteractions.AddRange(passiveInteractions);
    }

    public void SetInteraction(InteractionMode interaction, GameMode gameMode)
    {
        if (_interaction != null && _interaction.GetType() == interaction.GetType() && !_interaction.AllowReload)
        {
            //Debug.Error("[InteractionHandler] Interaction was not created: " + interaction.GetType().Name);
            return;
        }
        if (!_allowedInteractions.Contains(interaction.GetType()))
        {
            Debug.Error("[Interactionhandler] Invalid interaction type or this interaction is not allowed: " + interaction.GetType().Name);
            return;
        }

        if (_interaction != null) Interaction.OnDisable();
        interaction.GameMode = gameMode;
        interaction.OnEnable();
        _interaction = interaction;
    }

    public InteractionMode GetInteraction()
    {
        return Interaction;
    }

    public void Update(LocalAstronaut player)
    {

        if (!TryGiftHeldItem(player)) Interaction.Update(player);
        Spacebox.Game.Player.Targeting.BodyHighlight.Update(player, Spacebox.Game.GUI.PanelUI.CurrentSlot()?.Item);

        for (int i = 0; i < _passiveInteractions.Count; i++)
        {
            _passiveInteractions[i].Update(player, Interaction);
        }
    }

    private static bool TryGiftHeldItem(LocalAstronaut player)
    {
        if (!player.CanMove || !(Input.IsActionDown("use") || Input.IsActionDown("block_place"))) return false;

        var slot = Spacebox.Game.GUI.PanelUI.CurrentSlot();
        if (!Spacebox.Network.Players.ItemGiftNetwork.TryGive(player, slot)) return false;

        if (!slot.HasItem) player.SetInteraction(new InteractionDefault());
        return true;
    }

    public void Render(LocalAstronaut player)
    {
        Interaction.Render(player);
    }
}



