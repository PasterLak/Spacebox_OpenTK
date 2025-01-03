using Spacebox.Common;

namespace Spacebox.Game.Player;

public class InteractionHandler
{
    private InteractionMode _interaction;
    public InteractionMode Interaction {
        get => _interaction;
        set { SetInteraction(value, _gameMode); }
    }
    private readonly HashSet<Type> _allowedInteractions;

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
        
        if(_interaction != null) Interaction.OnDisable();
        interaction.GameMode = gameMode;
        interaction.OnEnable();
        _interaction = interaction;
    }

    public InteractionMode GetInteraction()
    {
        return Interaction;
    }

    public void Update(Astronaut player)
    {
        Interaction.Update(player);
    }
}